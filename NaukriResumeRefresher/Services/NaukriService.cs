using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text.Json;

namespace NaukriResumeRefresher.Services
{
    public class NaukriService
    {
        public async Task RunAsync()
        {
            Log.Information("Naukri job started");

            try
            {
                var config = new ConfigurationBuilder()
                               .AddJsonFile("appsettings.json")
                               .AddEnvironmentVariables()
                               .Build();

                Log.Information("Configuration loaded");

                var email = config["NaukriSettings:Email"];
                var password = config["NaukriSettings:Password"];
                var formKey = config["NaukriSettings:FormKey"];

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    Log.Error("Email or Password is missing in configuration");
                    throw new Exception("Invalid configuration");
                }

                var authService = new AuthService();
                var resumeService = new ResumeService();

                Log.Information("Attempting login...");
                var (client, _) = await authService.LoginAsync(email, password);

                Log.Information("Fetching profile ID...");
                var profileId = await GetProfileId(client);

                //var profileJson = await GetFullProfileAsync(client);

                //File.WriteAllText("profile.json", profileJson);

                Log.Information("Profile ID fetched: {ProfileId}", profileId);

                Log.Information("Uploading and updating resume...");
                await resumeService.UploadAndUpdateAsync(client, profileId, formKey);

                Log.Information("Naukri job completed successfully 🎉");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred in NaukriService");
                throw;
            }
        }

        private async Task<string> GetProfileId(HttpClient client)
        {
            try
            {
                var response = await client.GetAsync(
                    "https://www.naukri.com/cloudgateway-mynaukri/resman-aggregator-services/v0/users/self/dashboard");

                Log.Information("Dashboard API response: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error("Failed to fetch dashboard data");
                    throw new Exception("Dashboard API failed");
                }

                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);

                var profileId = doc.RootElement
                    .GetProperty("dashBoard")
                    .GetProperty("profileId")
                    .GetString();

                if (string.IsNullOrEmpty(profileId))
                {
                    Log.Error("Profile ID not found in response");
                    throw new Exception("Profile ID missing");
                }

                return profileId;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while fetching profile ID");
                throw;
            }
        }

        public async Task<string> GetFullProfileAsync(HttpClient client)
        {
            var response = await client.GetAsync(
                "https://www.naukri.com/cloudgateway-mynaukri/resman-aggregator-services/v0/users/self/dashboard"
            );

            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to fetch profile");

            var json = await response.Content.ReadAsStringAsync();

            return json; // raw profile data
        }
    }
}