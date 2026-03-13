// Application/UseCases/TopikWriting/Question54/DTOs/Question54RequestDto.cs
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.TopikWriting.Question54.DTOs
{
    public sealed class Question54RequestDto
    {
        [JsonPropertyName("userExamWritingAnswerId")]
        public string UserExamWritingAnswerId { get; set; } = string.Empty;
    }
}