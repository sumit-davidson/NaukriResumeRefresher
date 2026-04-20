using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NaukriResumeRefresher.Interfaces;
using NaukriResumeRefresher.Models;
using Serilog;
using System.Net.Http.Headers;
using System.Text;

namespace NaukriResumeRefresher.Services
{
    public class ResumeService : IResumeService
    {
        private readonly NaukriSettings _settings;

        public ResumeService(IOptions<NaukriSettings> options)
        {
            _settings = options.Value;
        }

        public async Task UploadAndUpdateAsync(HttpClient client, string profileId)
        {
            try
            {
                Log.Information("Starting resume upload process...");

                string resumeFolder = Path.Combine(AppContext.BaseDirectory, "Resume");
                Log.Information("Looking for resume in folder: {Folder}", resumeFolder);

                var filePath = Directory.GetFiles(resumeFolder, "*.pdf").FirstOrDefault();

                if (filePath == null)
                {
                    Log.Error("No resume file found in Resume folder");
                    throw new Exception("No resume found in Resume folder");
                }

                Log.Information("Resume found: {FileName}", Path.GetFileName(filePath));

                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                Log.Information("Resume file read successfully. Size: {Size} bytes", fileBytes.Length);

                string fileKey = "U" + Guid.NewGuid().ToString("N").Substring(0, 13);
                string fileName = Path.GetFileName(filePath);

                await UploadResumeFileAsync(client, fileBytes, fileName, fileKey);
                await UpdateResumeInProfileAsync(client, profileId, fileKey);

                Log.Information("Resume updated successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in ResumeService");
                throw;
            }
        }

        // ─────────────────────────────────────────────
        //  PRIVATE HELPERS
        // ─────────────────────────────────────────────

        private async Task UploadResumeFileAsync(HttpClient client, byte[] fileBytes, string fileName, string fileKey)
        {
            var multipart = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            multipart.Add(fileContent, "file", fileName);
            multipart.Add(new StringContent(_settings.FormKey), "formKey");
            multipart.Add(new StringContent(fileName), "fileName");
            multipart.Add(new StringContent("true"), "uploadCallback");
            multipart.Add(new StringContent(fileKey), "fileKey");

            Log.Information("Uploading resume file...");

            var response = await client.PostAsync("https://filevalidation.naukri.com/file", multipart);

            Log.Information("Upload response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                Log.Error("Resume file upload failed");
                throw new Exception("Upload failed");
            }

            Log.Information("Resume file uploaded successfully");
        }

        private async Task UpdateResumeInProfileAsync(HttpClient client, string profileId, string fileKey)
        {
            var updatePayload = new
            {
                textCV = new
                {
                    formKey = _settings.FormKey,
                    fileKey = fileKey,
                    textCvContent = (string)null
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://www.naukri.com/cloudgateway-mynaukri/resman-aggregator-services/v0/users/self/profiles/{profileId}/advResume");

            request.Content = new StringContent(
                JsonConvert.SerializeObject(updatePayload), Encoding.UTF8, "application/json");
            request.Headers.Add("x-http-method-override", "PUT");

            Log.Information("Updating resume in profile...");

            var response = await client.SendAsync(request);

            Log.Information("Update response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                Log.Error("Resume profile update failed");
                throw new Exception("Update failed");
            }

            Log.Information("Resume profile updated successfully");
        }
    }
}