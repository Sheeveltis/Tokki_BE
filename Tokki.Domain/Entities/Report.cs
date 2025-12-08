namespace Tokki.Domain.Entities
{
    public class Report
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } 
        public string Description { get; set; }
        public string? ImageUrl { get; set; } 
        public string? TargetUrl { get; set; } 
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public string? AdminReply { get; set; } 
        public bool UserHasRead { get; set; } = true; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
