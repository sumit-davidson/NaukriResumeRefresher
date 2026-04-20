namespace NaukriResumeRefresher.Interfaces
{
    public interface IMetricsService
    {
        Task LogDashboardMetricsAsync(HttpClient client);
    }
}