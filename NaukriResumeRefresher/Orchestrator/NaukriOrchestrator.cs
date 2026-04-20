using NaukriResumeRefresher.Interfaces;
using Serilog;

namespace NaukriResumeRefresher.Orchestrator
{
    public class NaukriOrchestrator
    {
        private readonly IAuthService _authService;
        private readonly IProfileService _profileService;
        private readonly IResumeService _resumeService;
        private readonly IMetricsService _metricsService;

        public NaukriOrchestrator(
            IAuthService authService,
            IProfileService profileService,
            IResumeService resumeService,
            IMetricsService metricsService)
        {
            _authService = authService;
            _profileService = profileService;
            _resumeService = resumeService;
            _metricsService = metricsService;
        }

        public async Task RunAsync()
        {
            Log.Information("========================================");
            Log.Information("      NAUKRI AUTO REFRESHER STARTED     ");
            Log.Information("========================================");

            // Step 1 - Login
            Log.Information("[STEP 1/6] Logging in...");
            var client = await _authService.LoginAsync();
            Log.Information("[STEP 1/6] Login successful");

            // Step 2 - Fetch Profile ID
            Log.Information("[STEP 2/6] Fetching profile ID...");
            var profileId = await _profileService.GetProfileIdAsync(client);
            Log.Information("[STEP 2/6] Profile ID fetched successfully");

            // Step 3 - Log Dashboard Metrics
            Log.Information("[STEP 3/6] Fetching dashboard metrics...");
            await _metricsService.LogDashboardMetricsAsync(client);

            // Step 4 - Update Profile
            Log.Information("[STEP 4/6] Running profile update tasks...");

            Log.Information("  -> Task 1: Rotating Key Skills");
            await _profileService.RotateKeySkillsAsync(client, profileId);

            Log.Information("  -> Task 2: Rotating Summary");
            await _profileService.RotateSummaryAsync(client, profileId);

            Log.Information("  -> Task 3: Updating Resume Headline");
            await _profileService.UpdateResumeHeadlineAsync(client, profileId);

            // Step 5 - Trigger Activity
            Log.Information("[STEP 5/6] Triggering activity ping...");
            await _profileService.TriggerActivityAsync(client);

            // Step 6 - Upload Resume
            Log.Information("[STEP 6/6] Uploading resume...");
            await _resumeService.UploadAndUpdateAsync(client, profileId);

            Log.Information("========================================");
            Log.Information("      ALL TASKS COMPLETED SUCCESSFULLY  ");
            Log.Information("========================================");
        }
    }
}