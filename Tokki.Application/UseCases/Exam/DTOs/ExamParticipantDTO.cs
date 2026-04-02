namespace Tokki.Application.UseCases.Exam.DTOs
{
    public class ExamParticipantDTO
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatar { get; set; }
        public int Score { get; set; }
        public int TotalCorrectQuestions { get; set; }
        public Dictionary<string, int> CorrectCountBySkill { get; set; } = new();
        public DateTime? SubmitTime { get; set; }
        public string UserExamId { get; set; } = string.Empty;
    }
}
