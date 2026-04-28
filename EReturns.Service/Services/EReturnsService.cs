using EDocuments.Contracts.Models;
using EDocuments.Contracts.Repositories;
using EDocuments.Contracts.Services;
using EDocuments.Contracts.Settings;
using EDocuments.Infrastructure.Helpers;
using EReturns.Service.Constants;
using EReturns.Service.Helpers;
using EReturns.Service.Settings;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace EReturns.Service.Services
{
    public class EReturnsService
    {
        private readonly IDocumentRepository _documentRepo;
        private readonly IAttributeRepository _attributeRepo;
        private readonly IXlApiService _xlApiService;
        private readonly IEmailService _emailService;
        private readonly ILogger<EReturnsService> _logger;
        private readonly AppSettings _appSettings;
        private readonly List<XlPrintSettings> _xlPrintSettings;
        public EReturnsService(IAttributeRepository attributeRepo, IDocumentRepository documentRepo, IXlApiService xlApiService, ILogger<EReturnsService> logger, IOptions<AppSettings> appSettings, IOptions<List<XlPrintSettings>> xlPrintSettings, IEmailService emailService)
        {
            _attributeRepo = attributeRepo;
            _documentRepo = documentRepo;
            _xlApiService = xlApiService;
            _logger = logger;
            _appSettings = appSettings.Value;
            _xlPrintSettings = xlPrintSettings.Value;
            _emailService = emailService;
        }

        public async Task GenerateAndSendEReturns(CancellationToken ct)
        {
            try
            {
                var returns = await _documentRepo.GetReturns();
                _logger.LogInformation("Retrieved {Count} returns to process.", returns.Count);

                if (returns.Count == 0)
                    return;

                var clientReturns = new Dictionary<string, List<(Return returnDoc, string pdfPath)>>();

                foreach (var returnDoc in returns.DistinctBy(i => i.Name))
                {
                    try
                    {
                        if (ct.IsCancellationRequested)
                        {
                            _logger.LogInformation("Cancellation requested. Stopping e-returns generation.");
                            break;
                        }

                        var printSettings = _xlPrintSettings.FirstOrDefault(s => s.DocumentType == returnDoc.Type && s.Language == returnDoc.Country);
                        if (printSettings == null)
                        {
                            printSettings = _xlPrintSettings.FirstOrDefault(s => s.DocumentType == returnDoc.Type && s.Language == "EN");
                            if (printSettings == null)
                            {
                                throw new Exception($"No print settings found for DocumentType={returnDoc.Type} and Language={returnDoc.Country} or fallback 'EN'.");
                            }
                        }

                        var filtrSql = $"(RLN_Typ={returnDoc.Type} AND RLN_Id={returnDoc.Id})";
                        string pdfPath = Path.Combine(AppContext.BaseDirectory, ServiceConstants.ExportFolder, returnDoc.FileName);

                        _xlApiService.GeneratePrint(printSettings, pdfPath, filtrSql);

                        if (!File.Exists(pdfPath))
                        {
                            _logger.LogError("Failed to generate PDF for document: {Document}. Expected file not found at path: {PdfPath}", returnDoc.Name, pdfPath);
                            continue;
                        }

                        _logger.LogInformation("Generated PDF for document: {Document} at path: {PdfPath}", returnDoc.Name, pdfPath);

                        var clientEmail = returnDoc.Email?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(e => e.Trim())
                            .FirstOrDefault();

                        if (string.IsNullOrWhiteSpace(clientEmail))
                        {
                            _logger.LogError("Invoice {Document} has no valid client email.", returnDoc.Name);
                            continue;
                        }

                        if (!clientReturns.ContainsKey(clientEmail))
                            clientReturns[clientEmail] = new List<(Return, string)>();

                        clientReturns[clientEmail].Add((returnDoc, pdfPath));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing Invoice document: {Document}.", returnDoc.Name);
                    }
                }

                // Send grouped emails
                foreach (var kvp in clientReturns)
                {
                    var clientEmail = kvp.Key;
                    var returnGroup = kvp.Value;
                    var attachments = returnGroup.Select(x => x.pdfPath).ToList();
                    var returnDoc = returnGroup.First().returnDoc;

                    List<string> to = returnDoc.Email.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .ToList();

                    string body = ReturnsEmailBuilder.BuildReturnBody(returnDoc.Country);
                    string subject = returnDoc.Country switch
                    {
                        "PL" => "Protokół zwrotu towarów",
                        "RO" => "Protocolul de retur al mărfurilor",
                        "DE" => "Protokoll für die rücksendung der ware",
                        "UA" => "Протокол повернення товарів",
                        _ => "Protocol returning the goods"
                    };

                    try
                    {
                        _emailService.Send(body, subject, to, attachments);
                        _logger.LogInformation("Successfully generated and sent e-returns(s) for client: {ClientEmail} with {Count} attachments.", clientEmail, attachments.Count);

                        try
                        {
                            foreach (var ret in returnGroup)
                            {
                                await _attributeRepo.UpdateAttribute("Mail zwrot towarów", ret.returnDoc.Id, ret.returnDoc.Type, 0, DateTime.Now.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to update attributes for returns(s) of client: {ClientEmail}.", clientEmail);
                        }
                    }
                    catch (SmtpException smtpEx) when (smtpEx.Message.Contains("Osiagnieto limit"))
                    {
                        DateTime now = DateTime.Now;
                        var delay = TimeSpan.FromHours(1) - TimeSpan.FromMinutes(now.Minute) - TimeSpan.FromSeconds(now.Second);
                        _logger.LogWarning($"Email sending limit reached. Waiting for next hour ({now.Add(delay):HH:mm}) before sending more.");

                        await Task.Delay(delay + TimeSpan.FromMinutes(5));
                        _emailService.Send(body, subject, to, attachments);
                    }
                    catch (SmtpException smtpEx) when (smtpEx.Message.Contains("ASCII local-parts"))
                    {
                        if (string.IsNullOrEmpty(returnDoc.RepresentativeEmail))
                        {
                            _logger.LogError(smtpEx, "Invalid email format for client: {ClientEmail}. No representative email provided. Skipping email sending for invoice {InvoiceName}.", clientEmail, returnDoc.Name);
                            continue;
                        }

                        _logger.LogError(smtpEx, "Invalid email format for client: {ClientEmail}. Sending email to representative {Representative}.", clientEmail, returnDoc.RepresentativeEmail);
                        subject = $"Błędny adres e-mail - {returnDoc.Name}";
                        body = ErrorEmailBuilder.BuildErrorBodyForRepresentative(returnDoc.Name, to);
                        to = new List<string> { returnDoc.RepresentativeEmail };

                        _emailService.Send(body, subject, to, null);
                    }
                    catch (FormatException ex)
                    {
                        if (string.IsNullOrEmpty(returnDoc.RepresentativeEmail))
                        {
                            _logger.LogError(ex, "Invalid email format for client: {ClientEmail}. No representative email provided. Skipping email sending for invoice {InvoiceName}.", clientEmail, returnDoc.Name);
                            continue;
                        }

                        _logger.LogError(ex, "Invalid email format for client: {ClientEmail}. Sending email to representative {Representative}.", clientEmail, returnDoc.RepresentativeEmail);
                        subject = $"Błędny adres e-mail - {returnDoc.Name}";
                        body = ErrorEmailBuilder.BuildErrorBodyForRepresentative(returnDoc.Name, to);
                        to = new List<string> { returnDoc.RepresentativeEmail };

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
