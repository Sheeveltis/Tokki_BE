using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Blogs.Commands.SaveOfficialBlog
{
    public class SaveOfficialBlogCommandHandler : IRequestHandler<SaveOfficialBlogCommand, OperationResult<string>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<SaveOfficialBlogCommandHandler> _logger;

        public SaveOfficialBlogCommandHandler(
            IBlogRepository blogRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<SaveOfficialBlogCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(SaveOfficialBlogCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var categoryExists = await _blogRepository.CategoryExistsAsync(request.CategoryId);
                if (!categoryExists)
                {
                    return OperationResult<string>.Failure(AppErrors.CategoryNotFound, 404, "Danh mục không tồn tại.");
                }

                Blog blogEntity;
                bool isNew = string.IsNullOrEmpty(request.Id);

                if (!isNew)
                {
                    blogEntity = await _blogRepository.GetByIdAsync(request.Id!);
                    if (blogEntity == null)
                    {
                        return OperationResult<string>.Failure(AppErrors.BlogNotFound, 404, "Không tìm thấy bài viết để cập nhật.");
                    }
                }
                else
                {
                    string newId = _idGeneratorService.GenerateCustom(10);
                    blogEntity = new Blog
                    {
                        Id = newId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        AuthorId = request.AdminId,
                        IsOfficial = true // Đánh dấu là bài viết chính thức
                    };
                }

                blogEntity.Title = request.Title;
                blogEntity.Slug = SlugHelper.GenerateSlug(request.Title, blogEntity.Id);
                blogEntity.ThumbnailUrl = request.ThumbnailUrl;
                blogEntity.Content = request.Content;
                blogEntity.ShortDescription = request.ShortDescription;
                blogEntity.CategoryId = request.CategoryId;
                blogEntity.Status = BlogStatus.Draft; // Luôn về nháp khi dùng API lưu nháp
                blogEntity.UpdatedAt = DateTimeOffset.UtcNow;
                
                // Cập nhật Tags (Official tags thường không cần qua AI check khi save nháp bởi Admin)
                var tags = await _blogRepository.GetOrCreateTagsAsync(request.Tags, true);
                blogEntity.Tags = tags;

                if (isNew)
                {
                    await _blogRepository.AddAsync(blogEntity);
                }
                else
                {
                    await _blogRepository.UpdateAsync(blogEntity);
                }

                await _blogRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(blogEntity.Id, isNew ? 201 : 200, "Đã lưu bản nháp bài viết chính thức thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu nháp bài viết chính thức: {Message}", ex.Message);
                return OperationResult<string>.Failure(AppErrors.ServerError, 500, "Lỗi hệ thống khi lưu bản nháp.");
            }
        }
    }
}
