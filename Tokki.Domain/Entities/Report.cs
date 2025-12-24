using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    public class Report
    {
        public string Id { get; set; }
        public string UserId { get; set; } 
        public string Description { get; set; }
        public string? ImageUrl { get; set; } 
        public string? TargetUrl { get; set; } 
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public string? AdminReply { get; set; } 
        public bool UserHasRead { get; set; } = true; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string? QuestionBankId { get; set; }
        public string? VocabularyId { get; set; }

        [ForeignKey(nameof(QuestionBankId))]
        public virtual QuestionBank? QuestionBank { get; set; }

        [ForeignKey(nameof(VocabularyId))]
        public virtual Vocabulary? Vocabulary { get; set; }
    }
}
