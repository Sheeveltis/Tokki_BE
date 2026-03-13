// Application/UseCases/TopikWriting/Question53/DTOs/Question53ResultDto.cs
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.TopikWriting.Question53.DTOs
{
    public sealed class Question53ResultDto
    {
        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("feedback")]
        public JsonElement Feedback { get; set; }
    }
}