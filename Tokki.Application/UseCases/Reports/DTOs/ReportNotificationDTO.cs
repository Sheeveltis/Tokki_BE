namespace Tokki.Application.UseCases.Reports.DTOs
{
    public class ReportNotificationDTO
    {
        public string ReportId { get; set; }
        public int Status { get; set; }
        public string AdminReply { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? TargetUrl { get; set; }
    }
}