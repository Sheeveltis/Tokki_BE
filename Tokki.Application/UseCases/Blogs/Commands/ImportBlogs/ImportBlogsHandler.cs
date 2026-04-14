using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Blogs.Commands.ImportBlogs
{
    public class ImportBlogsHandler : IRequestHandler<ImportBlogsCommand, OperationResult<bool>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IExcelService _excelService;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ImportBlogsHandler> _logger;

        public ImportBlogsHandler(
            IBlogRepository blogRepository,
            ICategoryRepository categoryRepository,
            IExcelService excelService,
            IIdGeneratorService idGeneratorService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ImportBlogsHandler> logger)
        {
            _blogRepository = blogRepository;
            _categoryRepository = categoryRepository;
            _excelService = excelService;
            _idGeneratorService = idGeneratorService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(ImportBlogsCommand request, CancellationToken cancellationToken)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return OperationResult<bool>.Failure(AppErrors.UserUnauthorized);
            }

            var blogData = await _excelService.ExtractBlogDataAsync(request.File);
            if (blogData == null || !blogData.Any())
            {
                return OperationResult<bool>.Failure("File Excel không có dữ liệu bài viết hợp lệ.", 400);
            }

            await using var transaction = await _blogRepository.BeginTransactionAsync(cancellationToken);

            try
            {
                // Load categories for mapping name to ID
                var categories = await _categoryRepository.GetAllAsync(cancellationToken);
                var categoryMap = categories.ToDictionary(c => c.Name.ToLowerInvariant().Trim(), c => c.Id);

                var newBlogs = new List<Blog>();
                var processedTitles = new HashSet<string>();

                foreach (var data in blogData)
                {
                    if (string.IsNullOrWhiteSpace(data.Title)) continue;

                    var normalizedTitle = data.Title.ToLowerInvariant().Trim();
                    if (processedTitles.Contains(normalizedTitle)) continue;

                    // MAPPING CATEGORY
                    string? categoryId = null;
                    if (!string.IsNullOrWhiteSpace(data.CategoryName))
                    {
                        var normCatName = data.CategoryName.ToLowerInvariant().Trim();
                        if (categoryMap.ContainsKey(normCatName))
                        {
                            categoryId = categoryMap[normCatName];
                        }
                        else
                        {
                            _logger.LogWarning("Không tìm thấy Category '{Name}' cho bài viết '{Title}'. Bỏ qua dòng này.", data.CategoryName, data.Title);
                            continue;
                        }
                    }

                    // TAGS
                    var tagList = string.IsNullOrWhiteSpace(data.Tags) 
                        ? new List<string>() 
                        : data.Tags.Split(',').Select(t => t.Trim()).ToList();
                    
                    var tags = await _blogRepository.GetOrCreateTagsAsync(tagList);

                    string newId = _idGeneratorService.GenerateCustom(10);
                    string slug = !string.IsNullOrWhiteSpace(data.Slug) 
                        ? data.Slug 
                        : SlugHelper.GenerateSlug(data.Title, newId);

                    newBlogs.Add(new Blog
                    {
                        Id = newId,
                        Title = data.Title.Trim(),
                        Slug = slug,
                        ThumbnailUrl = data.ThumbnailUrl,
                        ShortDescription = data.ShortDescription,
                        Content = data.Content,
                        CategoryId = categoryId!,
                        Status = BlogStatus.Published, // Import bởi Admin/Staff -> Duyệt luôn
                        AuthorId = userId,
                        Tags = tags,
                        CreatedAt = DateTimeOffset.UtcNow,
                        ViewCount = 0
                    });

                    processedTitles.Add(normalizedTitle);
                }

                if (newBlogs.Any())
                {
                    await _blogRepository.AddRangeAsync(newBlogs, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                return OperationResult<bool>.Success(true, 201, $"Đã nhập thành công {newBlogs.Count} bài viết mới.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi khi import blog. Rollback thành công.");
                return OperationResult<bool>.Failure($"Lỗi hệ thống: {ex.Message}", 500);
            }
        }
    }
}
