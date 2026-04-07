using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Blogs.Queries.ExportBlogs
{
    public class ExportBlogsHandler : IRequestHandler<ExportBlogsQuery, OperationResult<byte[]>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IExcelService _excelService;

        public ExportBlogsHandler(IBlogRepository blogRepository, IExcelService excelService)
        {
            _blogRepository = blogRepository;
            _excelService = excelService;
        }

        public async Task<OperationResult<byte[]>> Handle(ExportBlogsQuery request, CancellationToken cancellationToken)
        {
            var blogs = await _blogRepository.GetAllWithDetailsAsync(cancellationToken);

            var exportData = blogs.Select(b => new BlogExcelDTO
            {
                Title = b.Title,
                ThumbnailUrl = b.ThumbnailUrl,
                ShortDescription = b.ShortDescription,
                Content = b.Content,
                CategoryName = b.Category?.Name ?? "Không có danh mục",
                Tags = string.Join(", ", b.Tags.Select(t => t.Name)),
                Slug = b.Slug
            }).ToList();

            var fileBytes = await _excelService.ExportBlogsToExcelAsync(exportData, "Blogs");

            return OperationResult<byte[]>.Success(fileBytes);
        }
    }
}
