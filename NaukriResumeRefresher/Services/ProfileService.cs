using NaukriResumeRefresher.Interfaces;
using NaukriResumeRefresher.Models;
using Newtonsoft.Json;
using Serilog;
using System.Text;
using System.Text.Json;

namespace NaukriResumeRefresher.Services
{
    public class ProfileService : IProfileService
    {
        private readonly NaukriSettings _settings;

        public ProfileService(NaukriSettings settings)
        {
            _settings = settings;
        }
        public async Task<string> GetProfileIdAsync(HttpClient client)
        {
            Log.Information("Fetching profile ID...");

            var response = await client.GetAsync(
                "https://www.naukri.com/cloudgateway-mynaukri/resman-aggregator-services/v0/users/self/dashboard");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Dashboard API failed with status: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            var profileId = doc.RootElement
                .GetProperty("dashBoard")
                .GetProperty("profileId")
                .GetString();

            Log.Information("Profile ID fetched successfully: {ProfileId}", profileId);

            return profileId;
        }

        public async Task RotateKeySkillsAsync(HttpClient client, string profileId)
        {
            Log.Information("[KeySkills] Fetching current skills...");

            dynamic fullProfile = await GetFullProfileAsync(client);
            string keySkillsRaw = (string)fullProfile.profile[0].keySkills;
            Log.Information("[KeySkills] Current: {Skills}", keySkillsRaw);

            var rng = new Random();
            var shuffled = keySkillsRaw
                .Split(',')
                .Select(s => s.Trim())
                .OrderBy(_ => rng.Next())
                .ToList();

            var newKeySkills = string.Join(",", shuffled);
            Log.Information("[KeySkills] Shuffled: {Skills}", newKeySkills);

            var payload = new
            {
                profileId = profileId,
                profile = new { keySkills = newKeySkills }
            };

            await SendProfileUpdateAsync(client, payload);
            Log.Information("[KeySkills] Rotated successfully");
        }

        public async Task RotateSummaryAsync(HttpClient client, string profileId)
        {
            Log.Information("[Summary] Fetching current summary...");

            dynamic fullProfile = await GetFullProfileAsync(client);
            string currentSummary = (string)fullProfile.profile[0].summary;
            Log.Information("[Summary] Current: {Summary}", currentSummary);

            string newSummary = TweakSummary(currentSummary);
            Log.Information("[Summary] Updated: {Summary}", newSummary);

            var payload = new
            {
                profileId = profileId,
                profile = new { summary = newSummary }
            };

            await SendProfileUpdateAsync(client, payload);
            Log.Information("[Summary] Updated successfully");
        }

        public async Task UpdateResumeHeadlineAsync(HttpClient client, string profileId)
        {
            Log.Information("[Headline] Fetching current headline...");

            dynamic fullProfile = await GetFullProfileAsync(client);
            string currentHeadline = (string)fullProfile.profile[0].resumeHeadline;
            Log.Information("[Headline] Current: {Headline}", currentHeadline);

            string newHeadline = TweakHeadline(currentHeadline);
            Log.Information("[Headline] Updated: {Headline}", newHeadline);

            var payload = new
            {
                profileId = profileId,
                profile = new { resumeHeadline = newHeadline }
            };

            await SendProfileUpdateAsync(client, payload);
            Log.Information("[Headline] Updated successfully");
        }

        public async Task TriggerActivityAsync(HttpClient client)
        {
            Log.Information("Triggering activity ping...");

            await client.GetAsync(
                "https://www.naukri.com/cloudgateway-mynaukri/resman-aggregator-services/v0/users/self/dashboard");

            await RandomDelayAsync();

            await client.GetAsync("https://www.naukri.com/mnjuser/profile");

            Log.Information("[Activity] Activity ping sent");
        }

        // ─────────────────────────────────────────────
        //  PRIVATE HELPERS
        // ─────────────────────────────────────────────

        private async Task<dynamic> GetFullProfileAsync(HttpClient client)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://www.naukri.com/cloudgateway-mynaukri/resman-aggregator-services/v2/users/self?expand_level=4");

            request.Headers.Add("appid", "105");
            request.Headers.Add("clientid", "d3skt0p");
            request.Headers.Add("systemid", "Naukri");
            request.Headers.Add("x-requested-with", "XMLHttpRequest");
            request.Headers.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Referer", "https://www.naukri.com/mnjuser/profile");

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"GetFullProfile failed with status: {response.StatusCode}");

            return JsonConvert.DeserializeObject(body);
        }

        private async Task SendProfileUpdateAsync(HttpClient client, object payload)
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://www.naukri.com/cloudgateway-mynaukri/resman-aggregator-services/v1/users/self/fullprofiles");

            request.Headers.Add("appid", "105");
            request.Headers.Add("clientid", "d3skt0p");
            request.Headers.Add("systemid", "Naukri");
            request.Headers.Add("x-requested-with", "XMLHttpRequest");
            request.Headers.Add("x-http-method-override", "PUT");
            request.Headers.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Origin", "https://www.naukri.com");
            request.Headers.Add("Referer", "https://www.naukri.com/mnjuser/profile?action=modalOpen");

            request.Content = new StringContent(
                JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Log.Error("Profile update failed. Status: {Status}, Body: {Body}", response.StatusCode, error);
                throw new Exception("Profile update failed");
            }
        }

        private string TweakSummary(string original)
        {
            var rng = new Random();

            //var swaps = new List<(string from, string to)>
            //{
            //    ("around 3 years",                          "approximately 3 years"),
            //    ("approximately 3 years",                   "around 3 years"),
            //    ("application support",                     "production support"),
            //    ("production support",                      "application support"),
            //    ("system enhancement",                      "system improvement"),
            //    ("system improvement",                      "system enhancement"),
            //    ("resolving production issues",             "fixing production issues"),
            //    ("fixing production issues",                "resolving production issues"),
            //    ("Focused on",                              "Committed to"),
            //    ("Committed to",                            "Focused on"),
            //    ("stable, maintainable",                    "maintainable, stable"),
            //    ("maintainable, stable",                    "stable, maintainable"),
            //    ("scalable backend solutions",              "scalable and reliable backend solutions"),
            //    ("scalable and reliable backend solutions", "scalable backend solutions"),
            //    ("participating in",                        "involved in"),
            //    ("involved in",                             "participating in"),
            //    ("proper design patterns",                  "industry-standard design patterns"),
            //    ("industry-standard design patterns",       "proper design patterns"),
            //    ("Web API,MVC",                             "Web API, MVC"),
            //    ("Web API, MVC",                            "Web API,MVC"),
            //};



            var applicabl = _settings.SummarySwaps
                .Where(s => original.Contains(s.From))
                .OrderBy(_ => rng.Next())
                .Take(rng.Next(1, 3))
                .ToList();

            string result = original;   
            foreach (var swap in applicable)
            {
                result = result.Replace(swap.From, swap.To);
                Log.Information("[Summary] Swap: '{From}' -> '{To}'", swap.From, swap.To);
            }

            return result;
        }

        private string TweakHeadline(string original)
        {
            var rng = new Random();

            //var swaps = new List<(string from, string to)>
            //{
            //    ("Results-oriented",                     "Result-driven"),
            //    ("Result-driven",                        "Results-oriented"),
            //    ("extensive experience",                 "strong experience"),
            //    ("strong experience",                    "extensive experience"),
            //    ("Proven track record",                  "Demonstrated track record"),
            //    ("Demonstrated track record",            "Proven track record"),
            //    ("designing and developing",             "developing and designing"),
            //    ("developing and designing",             "designing and developing"),
            //    ("scalable web applications",            "scalable and robust web applications"),
            //    ("scalable and robust web applications", "scalable web applications"),
            //    ("implementing database integration",    "handling database integration"),
            //    ("handling database integration",        "implementing database integration"),
            //    ("delivering high-quality code",         "writing high-quality code"),
            //    ("writing high-quality code",            "delivering high-quality code"),
            //    ("ASP.NET, C#, and MVC",                "C#, ASP.NET, and MVC"),
            //    ("C#, ASP.NET, and MVC",                "ASP.NET, C#, and MVC"),
            //};

            var applicable = _settings.HeadlineSwaps
                .Where(s => original.Contains(s.From))
                .OrderBy(_ => rng.Next())
                .Take(rng.Next(1, 3))
                .ToList();

            string result = original;
            foreach (var swap in applicable)
            {
                result = result.Replace(swap.From, swap.To);
                Log.Information("[Headline] Swap: '{From}' -> '{To}'", swap.From, swap.To);
            }

            return result;
        }

        private async Task RandomDelayAsync()
        {
            var rng = new Random();
            var seconds = rng.Next(3, 9);
            Log.Information("[Delay] Waiting {Seconds}s...", seconds);
            await Task.Delay(TimeSpan.FromSeconds(seconds));
        }
    }
}