namespace Tokki.Application.UseCases.PronunciationRule.DTOs
{
    public class PronunciationRuleAdminDTO
    {
        public string PronunciationRuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public int TotalExamples { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
