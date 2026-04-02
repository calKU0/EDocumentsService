using EDocuments.Contracts.Services;
using EExportDeclaration.Service.Constants;
using EExportDeclaration.Service.Services;
using EExportDeclaration.Service.Settings;
using Microsoft.Extensions.Options;

namespace EExportDeclaration.Service
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
                if (_appSettings.GeneratingDay != DateTime.Now.Day)
                {
                    _logger.LogInformation("Current day {CurrentHour} does not match generating day {GeneratingHour}. Waiting until generating day.", DateTime.Now.Day, _appSettings.GeneratingDay);
                    await Task.Delay(interval, stoppingToken);
                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var printingService = scope.ServiceProvider.GetRequiredService<EExportDeclarationService>();
                    var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

                    await printingService.GenerateAndSendExportDeclarations(stoppingToken);

                    string declarationsPath = Path.Combine(AppContext.BaseDirectory, ServiceConstants.ExportDeclarationFolder);
                    fileService.DeleteFilesFromFolder(declarationsPath, ".pdf");
                    _logger.LogInformation("Files deleted from {Path}", declarationsPath);

                    _lastRun = DateTime.Now;
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
