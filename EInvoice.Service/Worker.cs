using EDocuments.Contracts.Services;
using EInvoice.Service.Services;
using EInvoice.Service.Settings;
using Microsoft.Extensions.Options;

namespace EInvoice.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly AppSettings _appSettings;
        private DateTime _lastRun = DateTime.MinValue;

        public Worker(
            ILogger<Worker> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _appSettings = appSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var interval = TimeSpan.FromMinutes(_appSettings.WorkerIntervalMinutes);

                if (_lastRun.Date >= DateTime.Now.Date)
                {
                    _logger.LogInformation("Daily job already executed today at: {Time}. Waiting until next day.", _lastRun);
                    await Task.Delay(interval, stoppingToken);
                    continue;
                }
                if (_appSettings.GeneratingHour != DateTime.Now.Hour)
                {
                    _logger.LogInformation("Current hour {CurrentHour} does not match generating hour {GeneratingHour}. Waiting until generating hour.", DateTime.Now.Hour, _appSettings.GeneratingHour);
                    await Task.Delay(interval, stoppingToken);
                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var printingService = scope.ServiceProvider.GetRequiredService<EInvoiceService>();
                    var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

                    await printingService.GenerateAndSendEInvoices(stoppingToken);
                    fileService.BackupFiles(_appSettings.InvoicesPath, _appSettings.BackupPath);
                    _logger.LogInformation("Files backed up from {Source} to {Destination}", _appSettings.InvoicesPath, _appSettings.BackupPath);

                    _logger.LogInformation("Daily job executed at: {Time}", DateTime.Now);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker execution failed.");
                }
                finally
                {
                    await Task.Delay(interval, stoppingToken);
                }
            }
        }
    
    }
}
