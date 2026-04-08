using EDocuments.Contracts.Services;
using EWZ.Service.Constants;
using EWZ.Service.Services;
using EWZ.Service.Settings;
using Microsoft.Extensions.Options;

namespace EWZ.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly AppSettings _appSettings;
        private DateTime _lastRun = DateTime.MinValue;
        private int _xlSessionId;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _appSettings = appSettings.Value;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var xlApiService = scope.ServiceProvider.GetRequiredService<IXlApiService>();
                _xlSessionId = xlApiService.Login();
                _logger.LogInformation("Logged in to XL API with session id: {SessionId}", _xlSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to login to XL API during service startup.");
                throw;
            }

            await base.StartAsync(cancellationToken);
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
                    var printingService = scope.ServiceProvider.GetRequiredService<EWZService>();
                    var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

                    await printingService.GenerateAndSendEWZs(stoppingToken);

                    string wzPath = Path.Combine(AppContext.BaseDirectory, ServiceConstants.WZFolder);
                    fileService.DeleteFilesFromFolder(wzPath, ".pdf");
                    _logger.LogInformation("Files deleted from {Path}", wzPath);

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

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_xlSessionId != 0)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var xlApiService = scope.ServiceProvider.GetRequiredService<IXlApiService>();
                    xlApiService.Logout(_xlSessionId);
                    _logger.LogInformation("Logged out from XL API session id: {SessionId}", _xlSessionId);
                    _xlSessionId = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to logout from XL API during service shutdown.");
            }

            await base.StopAsync(cancellationToken);
        }

    }
}
