

namespace Tokki.Application.UseCases.QuestionBanks.DTOs
{
    public class CreateQuestionOptionDto
    {
        public string KeyOption { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsCorrect { get; set; }
    }
}
