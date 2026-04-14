using System.Collections.Generic;

namespace Tokki.Application.UseCases.Excel.DTOs
{
    public class BlogExcelDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string ShortDescription { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty; // Để user nhập tên category cho dễ
        public string Tags { get; set; } = string.Empty; // Phân cách bằng dấu phẩy
        public string? Slug { get; set; }
    }
}
