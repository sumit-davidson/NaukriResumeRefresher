using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NaukriResumeRefresher.Interfaces;
using NaukriResumeRefresher.Models;
using NaukriResumeRefresher.Orchestrator;
using NaukriResumeRefresher.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "Logs", "log-.txt"),
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureServices((context, services) =>
    {
        // Config
        services.Configure<NaukriSettings>(
            context.Configuration.GetSection("NaukriSettings"));

        // HttpClient
        services.AddHttpClient();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IResumeService, ResumeService>();
        services.AddScoped<IMetricsService, MetricsService>();

        // Orchestrator
        services.AddScoped<NaukriOrchestrator>();
    })
    .Build();

try
{
    Log.Information("Application Started");
    var orchestrator = host.Services.GetRequiredService<NaukriOrchestrator>();
    await orchestrator.RunAsync();
    Log.Information("Application completed successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "Application failed");
}
finally
{
    Log.CloseAndFlush();
}