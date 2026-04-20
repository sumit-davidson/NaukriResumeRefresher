namespace NaukriResumeRefresher.Models
{
    public class NaukriSettings
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FormKey { get; set; } = string.Empty;
        public List<SwapPair> SummarySwaps { get; set; } = new();
        public List<SwapPair> HeadlineSwaps { get; set; } = new();
    }
    public class SwapPair
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
    }
}
