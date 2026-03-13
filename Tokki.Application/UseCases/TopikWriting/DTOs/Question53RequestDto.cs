// Application/UseCases/TopikWriting/Question53/DTOs/Question53RequestDto.cs
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.TopikWriting.Question53.DTOs
{
    public sealed class Question53RequestDto
    {
        [JsonPropertyName("userExamWritingAnswerId")]
        public string UserExamWritingAnswerId { get; set; } = string.Empty;
    }
}