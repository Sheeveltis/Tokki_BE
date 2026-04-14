namespace Tokki.Application.UseCases.StatisticBlog.DTOs
{
    public class DashboardStatDTO
    {
        public int TotalBlogs { get; set; }
        public long TotalViews { get; set; }
        public int TotalPublished { get; set; }
        public int TotalInternalBlogs { get; set; }     // Do Staff/Admin tạo
        public int TotalCommunityBlogs { get; set; }    // Do User tạo
        public int TotalPendingBlogs { get; set; }      // Bài cần duyệt
    }
}
