namespace Tokki.Infrastructure.Services.Gemini
{
    public sealed class GeminiOptions
    {
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
        public string Model { get; set; }
        public string? ApiKey { get; set; }
    }
}
