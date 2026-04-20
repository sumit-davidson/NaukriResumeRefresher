using Microsoft.Extensions.Options;
using NaukriResumeRefresher.Interfaces;
using NaukriResumeRefresher.Models;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NaukriResumeRefresher.Services
{
    public class AuthService : IAuthService
    {
        private readonly NaukriSettings _settings;

        public AuthService(IOptions<NaukriSettings> options)
        {
            _settings = options.Value;
        }

        public async Task<HttpClient> LoginAsync()
        {
            Log.Information("Starting login process...");

            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };

            // Note: We use handler directly here because we need
            // cookie container access after the request
            var client = new HttpClient(handler);

            SetDefaultHeaders(client);

            var payload = new { username = _settings.Email, password = _settings.Password };

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
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Log.Error("Login failed. Status: {StatusCode}, Body: {Body}", response.StatusCode, errorBody);
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

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                Log.Information("Token acquired and set successfully ✅");

                return client;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred during login process");
                throw;
            }
        }

        private void SetDefaultHeaders(HttpClient client)
        {
            client.DefaultRequestHeaders.Add("appid", "105");
            client.DefaultRequestHeaders.Add("clientid", "d3skt0p");
            client.DefaultRequestHeaders.Add("systemid", "jobseeker");
            client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.8");
            client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua",
                "\"Brave\";v=\"147\", \"Not.A/Brand\";v=\"8\", \"Chromium\";v=\"147\"");
            client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            client.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
            client.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
            client.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
            client.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
            client.DefaultRequestHeaders.Add("sec-gpc", "1");
            client.DefaultRequestHeaders.Add("Origin", "https://www.naukri.com");
            client.DefaultRequestHeaders.Add("Referer", "https://www.naukri.com/");
        }
    }
}