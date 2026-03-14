using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.TopikWriting.DTOs
{
    public sealed class Question51ResultDto
    {
        public JsonElement Feedback { get; set; }
        [JsonPropertyName("score")]
        public int Score { get; set; }
    }
}