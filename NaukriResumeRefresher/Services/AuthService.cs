using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Serilog;

namespace NaukriResumeRefresher.Services
{
    public class AuthService
    {
        public async Task<(HttpClient client, string token)> LoginAsync(string email, string password)
        {
            Log.Information("Starting login process...");

            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };

            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("appid", "105");
            client.DefaultRequestHeaders.Add("clientid", "d3skt0p");
            client.DefaultRequestHeaders.Add("systemid", "jobseeker");
            client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");

            var payload = new { username = email, password = password };

            try
            {
                Log.Information("Sending login request...");

                var response = await client.PostAsync(
                    "https://www.naukri.com/central-login-services/v1/login",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                );

                Log.Information("Login response received. StatusCode: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error("Login failed with status code: {StatusCode}", response.StatusCode);
                    throw new Exception("Login failed");
                }

                Log.Information("Login successful, extracting token...");

                var cookies = handler.CookieContainer.GetCookies(new Uri("https://www.naukri.com"));
                var token = cookies["nauk_at"]?.Value;

                if (string.IsNullOrEmpty(token))
                {
                    Log.Error("Token extraction failed. 'nauk_at' cookie not found.");
                    throw new Exception("Token missing");
                }

                Log.Information("Token acquired successfully");

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                return (client, token);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred during login process");
                throw;
            }
        }
    }
}