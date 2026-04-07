using EDocuments.Contracts.Models;
using EDocuments.Contracts.Repositories;
using EDocuments.Contracts.Services;
using EDocuments.Contracts.Settings;
using EDocuments.Infrastructure.Helpers;
using EInvoice.Service.Helpers;
using EInvoice.Service.Settings;
using Microsoft.Extensions.Options;
using Serilog;
using System.Net.Mail;

namespace EInvoice.Service.Services
{
    public class EInvoiceService
    {
        private readonly IDocumentRepository _documentRepo;
        private readonly IAttributeRepository _attributeRepo;
        private readonly IXlApiService _xlApiService;
        private readonly IEmailService _emailService;
        private readonly ILogger<EInvoiceService> _logger;
        private readonly AppSettings _appSettings;
        private readonly List<XlPrintSettings> _xlPrintSettings;
        public EInvoiceService(IAttributeRepository attributeRepo, IDocumentRepository documentRepo, IXlApiService xlApiService, ILogger<EInvoiceService> logger, IOptions<AppSettings> appSettings, IOptions<List<XlPrintSettings>> xlPrintSettings, IEmailService emailService)
        {
            _attributeRepo = attributeRepo;
            _documentRepo = documentRepo;
            _xlApiService = xlApiService;
            _logger = logger;
            _appSettings = appSettings.Value;
            _xlPrintSettings = xlPrintSettings.Value;
            _emailService = emailService;
        }

        public async Task GenerateAndSendEInvoices(CancellationToken ct)
        {
            int xlSessionId = 0;
            try
            {
                var invoices = await _documentRepo.GetInvoices();
                _logger.LogInformation("Retrieved {Count} invoices to process.", invoices.Count);

                var clientInvoices = new Dictionary<string, List<(Invoice invoice, string pdfPath)>>();
                xlSessionId = _xlApiService.Login();

                foreach (var invoice in invoices.DistinctBy(i => i.Name))
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancellation requested. Stopping e-invoice generation.");
                        break;
                    }

                    var printSettings = _xlPrintSettings.FirstOrDefault(s => s.DocumentType == invoice.Type && s.Language == invoice.Country && s.Stapled == (invoice.Country == "PL" ? invoice.IsStapled : false));
                    if (printSettings == null)
                    {
                        printSettings = _xlPrintSettings.FirstOrDefault(s => s.DocumentType == invoice.Type && s.Language == "EN");
                        if (printSettings == null)
                        {
                            throw new Exception($"No print settings found for DocumentType={invoice.Type} and Language={invoice.Country} or fallback 'EN'.");
                        }
                    }

                    var filtrSql = $"(TrN_GIDTyp={invoice.Type} AND TrN_GIDNumer={invoice.Id})";
                    string pdfPath = Path.Combine(_appSettings.InvoicesPath, invoice.FileName);

                    _xlApiService.GeneratePrint(printSettings, pdfPath, filtrSql);

                    if (!File.Exists(pdfPath))
                    {
                        _logger.LogError("Failed to generate PDF for document: {Document}. Expected file not found at path: {PdfPath}", invoice.Name, pdfPath);
                        continue;
                    }

                    _logger.LogInformation("Generated PDF for document: {Document} at path: {PdfPath}", invoice.Name, pdfPath);

                    var clientEmail = invoice.Email?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(clientEmail))
                    {
                        _logger.LogError("Invoice {Document} has no valid client email.", invoice.Name);
                        continue;
                    }

                    if (!clientInvoices.ContainsKey(clientEmail))
                        clientInvoices[clientEmail] = new List<(Invoice, string)>();

                    clientInvoices[clientEmail].Add((invoice, pdfPath));
                }

                // Send grouped emails
                foreach (var kvp in clientInvoices)
                {
                    var clientEmail = kvp.Key;
                    var invoiceGroup = kvp.Value;
                    var attachments = invoiceGroup.Select(x => x.pdfPath).ToList();
                    var invoice = invoiceGroup.First().invoice;

                    List<string> to = invoice.Email.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .ToList();

                    string body = InvoiceEmailBuilder.BuildEInvoiceBodyForGroup(invoiceGroup.Select(i => i.invoice).ToList());
                    string subject = invoice.Country == "PL" ? "E-faktura Gąska" : "E-Invoice Gaska";
                    try
                    {
                        _emailService.Send(body, subject, to, attachments);
                        _logger.LogInformation("Successfully generated and sent e-invoice(s) for client: {ClientEmail} with {Count} attachments.", clientEmail, attachments.Count);
                        foreach (var inv in invoiceGroup)
                        {
                            await _attributeRepo.UpdateAttribute("Mail e-faktura", inv.invoice.Id, inv.invoice.Type, 0, DateTime.Now.ToString());
                            await _attributeRepo.UpdateAttribute("Link e-faktura", inv.invoice.Id, inv.invoice.Type, 0, inv.pdfPath);
                            await _attributeRepo.UpdateAttribute("Mail e-faktura wyślij ponownie", inv.invoice.Id, inv.invoice.Type, 0, "NIE");
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
                    catch (FormatException ex)
                    {
                        if (string.IsNullOrEmpty(invoice.RepresentativeEmail))
                        {
                            _logger.LogError(ex, "Invalid email format for client: {ClientEmail}. No representative email provided. Skipping email sending for invoice {InvoiceName}.", clientEmail, invoice.Name);
                            continue;
                        }

                        _logger.LogError(ex, "Invalid email format for client: {ClientEmail}. Sending email to representative {Representative}.", clientEmail, invoice.RepresentativeEmail);
                        subject = $"Błędny adres e-mail - {invoice.Name}";
                        body = ErrorEmailBuilder.BuildErrorBodyForRepresentative(invoice.Name, to);
                        to = new List<string> { invoice.RepresentativeEmail };

                        _emailService.Send(body, subject, to, null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating and sending e-invoices.");
            }
            finally
            {
                try
                {
                    if (xlSessionId != 0)
                        _xlApiService.Logout(xlSessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error logging out of XL API.");
                }
            }
        }
    }
}
