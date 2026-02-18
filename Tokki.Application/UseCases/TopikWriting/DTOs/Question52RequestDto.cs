// Application/UseCases/TopikWriting/Question52/DTOs/Question52RequestDto.cs
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.TopikWriting.Question52.DTOs
{
    public sealed class Question52RequestDto
    {
        [JsonPropertyName("userExamWritingAnswerId")]
        public string UserExamWritingAnswerId { get; set; } = string.Empty;
    }
}