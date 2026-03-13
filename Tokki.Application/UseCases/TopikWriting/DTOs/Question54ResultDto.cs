// Application/UseCases/TopikWriting/Question54/DTOs/Question54ResultDto.cs
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.TopikWriting.Question54.DTOs
{
    public sealed class Question54ResultDto
    {
        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("feedback")]
        public JsonElement Feedback { get; set; }
    }
}