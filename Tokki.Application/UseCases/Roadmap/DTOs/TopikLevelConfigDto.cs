namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class TopikLevelConfigDto
    {
        public int TargetAimLevel { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int PassScore { get; set; }
        public int TotalScore { get; set; }
        public string ExamGroup { get; set; } = string.Empty;
        public string Listening { get; set; } = string.Empty;
        public string Reading { get; set; } = string.Empty;
        public string Writing { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
    }
}