using Microsoft.Extensions.Configuration;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class HelloWorld
{
    static async Task Main(string[] args)
    {


        string logsPath = Path.Combine(AppContext.BaseDirectory, "Logs");

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(logsPath,"log-.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Application Started");

            var service = new NaukriResumeRefresher.Services.NaukriService();
            await service.RunAsync();
            Log.Information("Application completed successfully");
        }
        catch(Exception ex)
        {
            Log.Error(ex, "Application failed");
        }
        finally
        {
                Log.CloseAndFlush();
            
        }
    }
}