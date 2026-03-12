// Application/UseCases/TopikWriting/Question51/DTOs/Question51RequestDto.cs
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.TopikWriting.Question51.DTOs
{
    public sealed class Question51RequestDto
    {
        [JsonPropertyName("userExamWritingAnswerId")]
        public string UserExamWritingAnswerId { get; set; } = string.Empty;
    }
}