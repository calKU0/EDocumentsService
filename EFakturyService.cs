using EFakturyService.Helpers;
using EFakturyService.Interfaces;
using EFakturyService.Models.Settings;
using EFakturyService.Services;
using RolgutXmlFromApi.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace EFakturyService
{
    public partial class EFakturyService : ServiceBase
    {
        private readonly AppSettings _appSettings;
        private readonly XlSettings _xlSettings;
        private readonly SmtpSettings _smtpSettings;

        private readonly IDataService _dataService;
        private readonly IXlService _xlService;
        private readonly IEmailSenderService _emailSenderService;

        private Timer _timer;
        private DateTime _lastRunDate;
        private bool _isProcessing = false;

        public EFakturyService()
        {
            InitializeComponent();

            _appSettings = AppSettingsLoader.LoadAppSettings();
            _xlSettings = AppSettingsLoader.LoadXlSettings();
            _smtpSettings = AppSettingsLoader.LoadSmtpSettings();

            _dataService = new DataService();
            _xlService = new XlService(_xlSettings);
            _emailSenderService = new EmailSenderService(_smtpSettings);

            _lastRunDate = DateTime.MinValue;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                LogConfig.Configure(_appSettings.LogsExpirationDate);

                _xlService.Login();

                _timer = new Timer(60000); // checking every one minute
                _timer.Elapsed += TimerElapsed;
                _timer.AutoReset = true;
                _timer.Start();

                Log.Information("Service started.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Błąd przy próbie uruchomienia serwisu.");
            }
        }

        protected override void OnStop()
        {
            try
            {
                _timer?.Stop();
                _timer?.Dispose();

                _xlService.Logout();

                Log.Information("Service stopped.");
                Log.CloseAndFlush();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Błąd przy próbie zatrzymania serwisu.");
            }
        }

        private async void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_isProcessing) return;

            _isProcessing = true;

            try
            {
                DateTime now = DateTime.Now;

                if (now.Hour == _appSettings.ServiceStartHour && _lastRunDate.Date < now.Date)
                {
                    Log.Information($"Serwis zaczyna daily wysyłkę e-faktur.");

                    // 1. Pobieram faktury z procedury.
                    var invoices = await _dataService.GetInvoices();
                    Log.Information($"Pobrano {invoices.Count} faktur. Rozpoczynam generowanie pdf oraz wysyłkę.");

                    foreach (var invoice in invoices)
                    {
                        try
                        {
                            // 2. Generuję plik pdf poprzez XLAPI
                            string pdfPath = _xlService.GenerateInvoicePdf(invoice);
                            if (!File.Exists(pdfPath))
                            {
                                Log.Warning($"Nie znaleziono pliku w {pdfPath}. Nie wysyłam e-faktury: {invoice.DocumentName}");
                                continue;
                            }

                            // 3. Wysyłam e-fakturę na maila
                            _emailSenderService.SendInvoiceEmail(invoice, pdfPath);

                            // 4. Aktualizuję atrybuty na fakturze
                            await _dataService.UpdateAttributes(invoice.GidType, invoice.GidNumber, _appSettings.BackupPath);

                            // 5. Generuję oświadczenie wywozowe z poprzedniego miesiąca dla kontrahenta gdy musze
                            if (invoice.ExportDeclaration)
                            {
                                string pdfExportPath = _xlService.GenerateExportDeclarationPdf(invoice);
                                if (!File.Exists(pdfExportPath))
                                {
                                    Log.Warning($"Nie znaleziono pliku w {pdfExportPath}. Nie wysyłam oświadczenia dla kontrahenta: {invoice.ClientName}.");
                                    continue;
                                }
                                // 6. Wysyłam oświadczenie na maila
                                _emailSenderService.SendExportDeclarationEmail(invoice, pdfExportPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Wystąpił błąd podczas wysyłania/generowania e-faktury {invoice.DocumentName} do klienta {invoice.ClientName} na adres email {invoice.Email}.");
                        }
                    }

                    // 7. Backupujemy pliki
                    FileHelpers.BackupFiles(_appSettings.BackupPath);

                    // Zaznaczamy, że dziś wykonaliśmy zadanie
                    _lastRunDate = now.Date;
                    Log.Information("Zakończono daily wysyłkę e-faktur.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Wystąpił błąd przy próbie pobierania e-faktur");
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}