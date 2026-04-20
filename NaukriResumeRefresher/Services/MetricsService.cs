using Newtonsoft.Json;
using NaukriResumeRefresher.Interfaces;
using Serilog;

namespace NaukriResumeRefresher.Services
{
    public class MetricsService : IMetricsService
    {
        public async Task LogDashboardMetricsAsync(HttpClient client)
        {
            Log.Information("Fetching dashboard metrics...");

            var response = await client.GetAsync(
                "https://www.naukri.com/cloudgateway-mynaukri/resman-aggregator-services/v0/users/self/dashboard");

            var body = await response.Content.ReadAsStringAsync();
            dynamic dashboard = JsonConvert.DeserializeObject(body);

            var profileViews = (int)dashboard.dashBoard.profileViewCount;
            var searchAppearances = (int)dashboard.dashBoard.totalSearchAppearancesCount;
            var recruiterDate = (string)dashboard.dashBoard.recruiterActionsLatestDate;
            var completeness = (int)dashboard.dashBoard.pc;
            var lastModified = (string)dashboard.dashBoard.mod_dt;
            var stale = (int)dashboard.dashBoard.modDtGtThanSixMonths;

            Log.Information("┌─────────────────────────────────┐");
            Log.Information("│        PROFILE METRICS          │");
            Log.Information("├─────────────────────────────────┤");
            Log.Information("│ Profile Views        : {Views}", profileViews);
            Log.Information("│ Search Appearances   : {Count}", searchAppearances);
            Log.Information("│ Last Recruiter Action: {Date}", recruiterDate);
            Log.Information("│ Completeness (PC)    : {PC}%", completeness);
            Log.Information("│ Last Modified        : {ModDt}", lastModified);
            Log.Information("│ Stale (>6 months)    : {Stale}", stale == 1 ? "YES" : "No");
            Log.Information("└─────────────────────────────────┘");

            if (completeness < 100)
                Log.Warning("Profile completeness is {PC}% — some sections are incomplete!", completeness);

            if (stale == 1)
                Log.Warning("Profile has not been updated in 6+ months!");

            var entry = $"{DateTime.Now:yyyy-MM-dd},{profileViews},{searchAppearances},{recruiterDate},{completeness}\n";
            await File.AppendAllTextAsync(
                Path.Combine(AppContext.BaseDirectory, "metrics.csv"), entry);

            Log.Information("Metrics saved to metrics.csv");
        }
    }
}