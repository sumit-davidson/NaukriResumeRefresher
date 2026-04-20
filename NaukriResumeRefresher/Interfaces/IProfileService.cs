namespace NaukriResumeRefresher.Interfaces
{
    public interface IProfileService
    {
        Task<string> GetProfileIdAsync(HttpClient client);
        Task RotateKeySkillsAsync(HttpClient client, string profileId);
        Task RotateSummaryAsync(HttpClient client, string profileId);
        Task UpdateResumeHeadlineAsync(HttpClient client, string profileId);
        Task TriggerActivityAsync(HttpClient client);
    }
}