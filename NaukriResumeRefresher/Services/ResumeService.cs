using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Serilog;

namespace NaukriResumeRefresher.Services
{
    public class ResumeService
    {
        public async Task UploadAndUpdateAsync(HttpClient client, string profileId, string formKey)
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

                var multipart = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                multipart.Add(fileContent, "file", "SumitRaghuvanshiResume.pdf");
                multipart.Add(new StringContent(formKey), "formKey");
                multipart.Add(new StringContent("SumitRaghuvanshiResume.pdf"), "fileName");
                multipart.Add(new StringContent("true"), "uploadCallback");
                multipart.Add(new StringContent(fileKey), "fileKey");

                Log.Information("Uploading resume to server...");

                var uploadResponse = await client.PostAsync("https://filevalidation.naukri.com/file", multipart);

                Log.Information("Upload response status: {StatusCode}", uploadResponse.StatusCode);

                if (!uploadResponse.IsSuccessStatusCode)
                {
                    Log.Error("Resume upload failed");
                    throw new Exception("Upload failed");
                }

                Log.Information("Resume uploaded successfully");

                var updatePayload = new
                {
                    textCV = new
                    {
                        formKey = formKey,
                        fileKey = fileKey,
                        textCvContent = (string)null
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post,
                    $"https://www.naukri.com/cloudgateway-mynaukri/resman-aggregator-services/v0/users/self/profiles/{profileId}/advResume");

                request.Content = new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json");
                request.Headers.Add("x-http-method-override", "PUT");

                Log.Information("Updating resume in profile...");

                var updateResponse = await client.SendAsync(request);

                Log.Information("Update response status: {StatusCode}", updateResponse.StatusCode);

                if (!updateResponse.IsSuccessStatusCode)
                {
                    Log.Error("Resume update failed");
                    throw new Exception("Update failed");
                }

                Log.Information("Resume updated successfully 🎉");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in ResumeService");
                throw;
            }
        }
    }
}