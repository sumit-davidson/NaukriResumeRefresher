namespace NaukriResumeRefresher.Interfaces
{
    public interface IResumeService
    {
        Task UploadAndUpdateAsync(HttpClient client, string profileId);
    }
}