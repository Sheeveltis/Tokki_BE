namespace Tokki.Application.UseCases.UserExam.DTOs
{
    public class GradingProgressResponse
    {
        public string UserExamId { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public bool IsCompleted { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
