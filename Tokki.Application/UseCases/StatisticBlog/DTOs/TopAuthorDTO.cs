namespace Tokki.Application.UseCases.StatisticBlog.DTOs
{
    public class TopAuthorDTO
    {
        public string AuthorId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int BlogCount { get; set; }
        public long TotalViews { get; set; }
    }
}
