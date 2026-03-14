// Application/UseCases/TopikWriting/Question52/DTOs/Question52ResultDto.cs
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.TopikWriting.Question52.DTOs
{
    public sealed class Question52ResultDto
    {
        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("feedback")]
        public JsonElement Feedback { get; set; }
    }
}