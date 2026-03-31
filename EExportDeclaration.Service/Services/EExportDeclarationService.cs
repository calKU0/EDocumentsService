using EDocuments.Contracts.Models;
using EDocuments.Contracts.Repositories;
using EDocuments.Contracts.Services;
using EDocuments.Contracts.Settings;
using EExportDeclaration.Service.Constants;
using EExportDeclaration.Service.Helpers;
using Microsoft.Extensions.Options;
using Serilog;
using System.Net.Mail;

namespace EExportDeclaration.Service.Services
{
    public class EExportDeclarationService
    {
        private readonly IDocumentRepository _documentRepo;
        private readonly IXlApiService _xlApiService;
        private readonly IEmailService _emailService;
        private readonly ILogger<EExportDeclarationService> _logger;
        private readonly List<XlPrintSettings> _xlPrintSettings;
        public EExportDeclarationService(IDocumentRepository documentRepo, IXlApiService xlApiService, ILogger<EExportDeclarationService> logger, IOptions<List<XlPrintSettings>> xlPrintSettings, IEmailService emailService)
        {
            _documentRepo = documentRepo;
            _xlApiService = xlApiService;
            _logger = logger;
            _xlPrintSettings = xlPrintSettings.Value;
            _emailService = emailService;
        }

        public async Task GenerateAndSendExportDeclarations(CancellationToken ct)
        {
            try
            {
                var declarations = await _documentRepo.GetExportDeclarations();
                _logger.LogInformation("Retrieved {Count} export declarations to process.", declarations.Count);

                _xlApiService.Login();

                foreach (var declaration in declarations)
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancellation requested. Stopping e-export declaration generation.");
                        break;
                    }

                    var printSettings = _xlPrintSettings.FirstOrDefault(s => s.DocumentType == declaration.ClientType && s.Language == declaration.ClientCountry);
                    if (printSettings == null)
                    {
                        printSettings = _xlPrintSettings.FirstOrDefault(s => s.DocumentType == declaration.ClientType && s.Language == "EN");
                        if (printSettings == null)
                        {
                            throw new Exception($"No print settings found for DocumentType={declaration.ClientType} and Language={declaration.ClientCountry} or fallback 'EN'.");
                        }
                    }

                    var filtrSql = $"(Knt_GIDTyp={declaration.ClientType} AND Knt_GIDFirma=449892 AND Knt_GIDNumer={declaration.ClientId})";
                    string pdfPath = Path.Combine(AppContext.BaseDirectory, ServiceConstants.ExportDeclarationFolder, declaration.FileName);

                    _xlApiService.GeneratePrint(printSettings, pdfPath, filtrSql);

                    if (!File.Exists(pdfPath))
                    {
                        _logger.LogError("Failed to generate PDF for export declaration for client: {Client}. Expected file not found at path: {PdfPath}", declaration.ClientName, pdfPath);
                        continue;
                    }
                    _logger.LogInformation("Generated PDF at path: {PdfPath}", pdfPath);

                    List<string> to = declaration.Email.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .ToList();

                    string body = ExportDeclarationEmailBuilder.BuildExportDeclarationBody(declaration.ClientCountry);
                    string subject = declaration.ClientCountry == "PL"
                        ? "Potwierdzenie dostawy towaru z terytorium Polski"
                        : "Confirmation of delivery the goods from the territory of Poland";

                    var attachments = new List<string> { pdfPath };
                    try
                    {
                        _emailService.Send(body, subject, to, attachments);
                        _logger.LogInformation("Successfully generated and sent e-export declarations for client: {ClientEmail} with {Count} attachments.", declaration.Email, attachments.Count);
                    }
                    catch (SmtpException smtpEx) when (smtpEx.Message.Contains("Osiagnieto limit"))
                    {
                        DateTime now = DateTime.Now;
                        var delay = TimeSpan.FromHours(1) - TimeSpan.FromMinutes(now.Minute) - TimeSpan.FromSeconds(now.Second);
                        Log.Warning($"Email sending limit reached. Waiting for next hour ({now.Add(delay):HH:mm}) before sending more.");

                        await Task.Delay(delay + TimeSpan.FromMinutes(5));
                        _emailService.Send(body, subject, to, attachments);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while generating and sending e-export declarations.");
            }
        }
    }
}
