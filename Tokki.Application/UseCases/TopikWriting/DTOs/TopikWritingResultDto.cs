using System.Text.Json;

namespace Tokki.Application.UseCases.TopikWriting.DTOs
{
    public sealed class TopikWritingResultDto
    {
        // JSON từ bước chấm/sửa bài
        public JsonElement Feedback { get; set; }
    }
}
