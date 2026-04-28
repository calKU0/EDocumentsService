using EDocuments.Contracts.Repositories;
using EDocuments.Contracts.Services;
using EDocuments.Contracts.Settings;
using EDocuments.Infrastructure.Data;
using EDocuments.Infrastructure.Logging;
using EDocuments.Infrastructure.Repositories;
using EDocuments.Infrastructure.Services;
using EReturns.Service;
using EReturns.Service.Constants;
using EReturns.Service.Services;
using EReturns.Service.Settings;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = ServiceConstants.ServiceName;
    })
    .UseSerilog((hostContext, _, loggerConfiguration) =>
    {
        loggerConfiguration.ConfigureServiceLogging(hostContext.Configuration, ServiceConstants.ServiceName);
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        // Configuration
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.Configure<XlApiSettings>(configuration.GetSection("XlApiSettings"));
        services.Configure<List<XlPrintSettings>>(configuration.GetSection("XlPrintSettings"));

        // Database context
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddSingleton(sp => new DapperContext(connectionString));

        // Repositories
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IAttributeRepository, AttributeRepository>();

        // Services
        services.AddScoped<IXlApiService, XlApiService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<EReturnsService>();

        // Background worker
        services.AddHostedService<Worker>();

        // Host options
        services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(15));
    })
    .Build();

host.Run();