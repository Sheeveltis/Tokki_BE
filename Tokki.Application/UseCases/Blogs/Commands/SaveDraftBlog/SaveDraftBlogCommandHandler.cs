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

namespace Tokki.Application.UseCases.Blogs.Commands.SaveDraftBlog
{
    public class SaveDraftBlogCommandHandler : IRequestHandler<SaveDraftBlogCommand, OperationResult<string>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<SaveDraftBlogCommandHandler> _logger;

        public SaveDraftBlogCommandHandler(
            IBlogRepository blogRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<SaveDraftBlogCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(SaveDraftBlogCommand request, CancellationToken cancellationToken)
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

                    if (blogEntity.AuthorId != request.UserId)
                    {
                        return OperationResult<string>.Failure(AppErrors.Forbidden, 403, "Bạn không có quyền chỉnh sửa bài viết này.");
                    }

                    // Chỉ cho phép lưu nháp trên bài viết đang ở trạng thái Nháp hoặc AI đã từ chối?
                    // Hoặc cho phép save nháp bất cứ lúc nào? Thường thì nháp là trạng thái riêng.
                    // Nếu đã đăng rồi thì Save nháp có thể coi như tạo 1 bản copy nháp? 
                    // Ở đây ta giả định là Save nháp cho bài viết chưa được Duyệt.
                    if (blogEntity.Status != BlogStatus.Draft && blogEntity.Status != BlogStatus.Rejected && blogEntity.Status != BlogStatus.AIRejected)
                    {
                         // Có thể cho phép lưu nháp kể cả khi đã đăng? (Hạ cấp về nháp)
                         // Thôi cứ cho lưu đi, người dùng muốn sửa lại mà chưa muốn public.
                    }
                }
                else
                {
                    string newId = _idGeneratorService.GenerateCustom(10);
                    blogEntity = new Blog
                    {
                        Id = newId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        AuthorId = request.UserId
                    };
                }

                blogEntity.Title = request.Title;
                blogEntity.Slug = SlugHelper.GenerateSlug(request.Title, blogEntity.Id);
                blogEntity.ThumbnailUrl = request.ThumbnailUrl;
                blogEntity.Content = request.Content;
                blogEntity.ShortDescription = request.ShortDescription;
                blogEntity.CategoryId = request.CategoryId;
                blogEntity.Status = BlogStatus.Draft; // Luôn về nháp khi dùng API này
                blogEntity.UpdatedAt = DateTimeOffset.UtcNow;
                
                // Cập nhật Tags
                var tags = await _blogRepository.GetOrCreateTagsAsync(request.Tags);
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

                return OperationResult<string>.Success(blogEntity.Id, isNew ? 201 : 200, "Đã lưu bản nháp thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu nháp bài viết: {Message}", ex.Message);
                return OperationResult<string>.Failure(AppErrors.ServerError, 500, "Lỗi hệ thống khi lưu bản nháp.");
            }
        }
    }
}
