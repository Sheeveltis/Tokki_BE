namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class SubmitExamRequestDto
    {
        public string ExamId { get; set; }
        public List<UserAnswerRequestDto> Answers { get; set; } = new List<UserAnswerRequestDto>();
    }

    public class UserAnswerRequestDto
    {
        public string QuestionId { get; set; }
        public string SelectedOptionId { get; set; }
    }
}