using EDocuments.Contracts.Models;
using EDocuments.Contracts.Repositories;
using EDocuments.Contracts.Services;
using EDocuments.Contracts.Settings;
using EDocuments.Infrastructure.Helpers;
using EWZ.Service.Constants;
using EWZ.Service.Helpers;
using Microsoft.Extensions.Options;
using Serilog;
using System.Net.Mail;

namespace EWZ.Service.Services
{
    public class EWZService
    {
        private readonly IDocumentRepository _documentRepo;
        private readonly IAttributeRepository _attributeRepo;
        private readonly IXlApiService _xlApiService;
        private readonly IEmailService _emailService;
        private readonly ILogger<EWZService> _logger;
        private readonly List<XlPrintSettings> _xlPrintSettings;
        public EWZService(IAttributeRepository attributeRepo, IDocumentRepository documentRepo, IXlApiService xlApiService, ILogger<EWZService> logger, IOptions<List<XlPrintSettings>> xlPrintSettings, IEmailService emailService)
        {
            _attributeRepo = attributeRepo;
            _documentRepo = documentRepo;
            _xlApiService = xlApiService;
            _logger = logger;
            _xlPrintSettings = xlPrintSettings.Value;
            _emailService = emailService;
        }

        public async Task GenerateAndSendEWZs(CancellationToken ct)
        {
            try
            {
                var wzList = await _documentRepo.GetWZDocuments();
                _logger.LogInformation("Retrieved {Count} WZ to process.", wzList.Count);

                if (wzList.Count == 0)
                    return;

                var clientInvoices = new Dictionary<string, List<(WZDocument wzDocument, string pdfPath)>>();

                foreach (var wz in wzList.DistinctBy(i => i.Name))
                {
                    try
                    {
                        if (ct.IsCancellationRequested)
                        {
                            _logger.LogInformation("Cancellation requested. Stopping e-WZ generation.");
                            break;
                        }

                        var printSettings = _xlPrintSettings.FirstOrDefault(s => s.DocumentType == wz.Type && s.Language == wz.Country);
                        if (printSettings == null)
                        {
                            printSettings = _xlPrintSettings.FirstOrDefault(s => s.DocumentType == wz.Type && s.Language == "EN");
                            if (printSettings == null)
                            {
                                throw new Exception($"No print settings found for DocumentType={wz.Type} and Language={wz.Country} or fallback 'EN'.");
                            }
                        }

                        var filtrSql = $"(TrN_GIDTyp={wz.Type} AND TrN_GIDNumer={wz.Id})";
                        string pdfPath = Path.Combine(AppContext.BaseDirectory, ServiceConstants.WZFolder, wz.FileName);

                        _xlApiService.GeneratePrint(printSettings, pdfPath, filtrSql);

                        if (!File.Exists(pdfPath))
                        {
                            _logger.LogError("Failed to generate PDF for document: {Document}. Expected file not found at path: {PdfPath}", wz.Name, pdfPath);
                            continue;
                        }

                        _logger.LogInformation("Generated PDF for document: {Document} at path: {PdfPath}", wz.Name, pdfPath);

                        var clientEmail = wz.Email?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(e => e.Trim())
                            .FirstOrDefault();

                        if (string.IsNullOrWhiteSpace(clientEmail))
                        {
                            _logger.LogError("WZ {Document} has no valid client email.", wz.Name);
                            continue;
                        }

                        if (!clientInvoices.ContainsKey(clientEmail))
                            clientInvoices[clientEmail] = new List<(WZDocument, string)>();

                        clientInvoices[clientEmail].Add((wz, pdfPath));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing WZ document: {Document}.", wz.Name);
                    }
                }

                // Send grouped emails
                foreach (var kvp in clientInvoices)
                {
                    var clientEmail = kvp.Key;
                    var wzGroup = kvp.Value;
                    var attachments = wzGroup.Select(x => x.pdfPath).ToList();
                    var wzDocument = wzGroup.First().wzDocument;

                    List<string> to = wzDocument.Email.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .ToList();

                    string body = WZEmailBuilder.BuildEWZBodyForGroup(wzGroup.Select(i => i.wzDocument).ToList());
                    string subject = wzDocument.Country == "PL" ? "E-WZ Gąska" : "E-WZ Gaska";
                    try
                    {
                        _emailService.Send(body, subject, to, attachments);
                        _logger.LogInformation("Successfully generated and sent e-WZ(s) for client: {ClientEmail} with {Count} attachments.", clientEmail, attachments.Count);
                        try
                        {
                            foreach (var wz in wzGroup)
                            {
                                await _attributeRepo.UpdateAttribute("Mail e-faktura", wz.wzDocument.Id, wz.wzDocument.Type, 0, DateTime.Now.ToString());
                                await _attributeRepo.UpdateAttribute("Link e-faktura", wz.wzDocument.Id, wz.wzDocument.Type, 0, wz.pdfPath);
                                await _attributeRepo.UpdateAttribute("Mail e-faktura wyślij ponownie", wz.wzDocument.Id, wz.wzDocument.Type, 0, "NIE");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to update attributes for invoice(s) of client: {ClientEmail}.", clientEmail);
                        }
                    }
                    catch (SmtpException smtpEx) when (smtpEx.Message.Contains("Osiagnieto limit"))
                    {
                        DateTime now = DateTime.Now;
                        var delay = TimeSpan.FromHours(1) - TimeSpan.FromMinutes(now.Minute) - TimeSpan.FromSeconds(now.Second);
                        Log.Warning($"Email sending limit reached. Waiting for next hour ({now.Add(delay):HH:mm}) before sending more.");

                        await Task.Delay(delay + TimeSpan.FromMinutes(5));
                        _emailService.Send(body, subject, to, attachments);
                    }
                    catch (SmtpException smtpEx) when (smtpEx.Message.Contains("ASCII local-parts"))
                    {
                        if (string.IsNullOrEmpty(wzDocument.RepresentativeEmail))
                        {
                            _logger.LogError(smtpEx, "Invalid email format for client: {ClientEmail}. No representative email provided. Skipping email sending for invoice {InvoiceName}.", clientEmail, wzDocument.Name);
                            continue;
                        }

                        _logger.LogError(smtpEx, "Invalid email format for client: {ClientEmail}. Sending email to representative {Representative}.", clientEmail, wzDocument.RepresentativeEmail);
                        subject = $"Błędny adres e-mail - {wzDocument.Name}";
                        body = ErrorEmailBuilder.BuildErrorBodyForRepresentative(wzDocument.Name, to);
                        to = new List<string> { wzDocument.RepresentativeEmail };

                        _emailService.Send(body, subject, to, null);
                    }
                    catch (FormatException ex)
                    {
                        if (string.IsNullOrEmpty(wzDocument.RepresentativeEmail))
                        {
                            _logger.LogError(ex, "Invalid email format for client: {ClientEmail}. No representative email provided. Skipping email sending for invoice {InvoiceName}.", clientEmail, wzDocument.Name);
                            continue;
                        }

                        _logger.LogError(ex, "Invalid email format for client: {ClientEmail}. Sending email to representative {Representative}.", clientEmail, wzDocument.RepresentativeEmail);
                        subject = $"Błędny adres e-mail - {wzDocument.Name}";
                        body = ErrorEmailBuilder.BuildErrorBodyForRepresentative(wzDocument.Name, to);
                        to = new List<string> { wzDocument.RepresentativeEmail };

                        _emailService.Send(body, subject, to, null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating and sending e-invoices.");
            }
        }
    }
}
