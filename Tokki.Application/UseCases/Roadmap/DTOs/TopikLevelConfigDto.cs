namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class TopikLevelConfigDto
    {
        public int TargetAimLevel { get; set; }
        public int PassScore { get; set; }
        public int TotalScore { get; set; }
        public string ExamGroup { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int EntranceExamType { get; set; }
        public string ConfigKey { get; set; } = string.Empty;
    }
}