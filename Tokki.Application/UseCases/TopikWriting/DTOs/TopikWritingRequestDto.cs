using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.TopikWriting.DTOs
{
    public sealed class TopikWritingRequestDto
    {
        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("targets")]
        public TargetsDto Targets { get; set; } = new();

        [JsonPropertyName("question")]
        public QuestionDto Question { get; set; } = new();
    }

    public sealed class TargetsDto
    {
        [JsonPropertyName("grammar")]
        public List<string> Grammar { get; set; } = new();

    }

    public sealed class QuestionDto
    {
        [JsonPropertyName("no")]
        public int No { get; set; }

        [JsonPropertyName("prompt")]
        public PromptDto Prompt { get; set; } = new();

        [JsonPropertyName("submission")]
        public SubmissionDto Submission { get; set; } = new();
    }

    public sealed class PromptDto
    {
        // text có thể là đề (nếu frontend OCR trước hoặc user nhập)
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        // user có thể gửi ảnh bất kỳ (ảnh chụp đề, ảnh biểu đồ, ảnh chữ…)
        [JsonPropertyName("images")]
        public List<ImageDto>? Images { get; set; } = new();
    }

    public sealed class ImageDto
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";
    }

    public sealed class SubmissionDto
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }
}
