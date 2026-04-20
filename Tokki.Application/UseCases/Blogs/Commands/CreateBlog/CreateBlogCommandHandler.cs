using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Hangfire;

namespace Tokki.Application.UseCases.Blogs.Commands.CreateBlog
{
    public class CreateBlogCommandHandler : IRequestHandler<CreateBlogCommand, OperationResult<string>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly AppNotificationHelper _notificationHelper;
        private readonly ILogger<CreateBlogCommandHandler> _logger;

        public CreateBlogCommandHandler(
            IBlogRepository blogRepository,
            IIdGeneratorService idGeneratorService,
            IBackgroundJobClient backgroundJobs,
            AppNotificationHelper notificationHelper,
            ILogger<CreateBlogCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _idGeneratorService = idGeneratorService;
            _backgroundJobs = backgroundJobs;
            _notificationHelper = notificationHelper;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
        {
            var categoryExists = await _blogRepository.CategoryExistsAsync(request.CategoryId);
            if (!categoryExists)
            {
                return OperationResult<string>.Failure(
                    AppErrors.CategoryNotFound,
                    404,
                    OperationMessages.NotFound("Danh mục")
                );
            }

            try
            {
                var tags = await _blogRepository.GetOrCreateTagsAsync(request.Tags, true);
                string newId = _idGeneratorService.GenerateCustom(10);
                string slug = SlugHelper.GenerateSlug(request.Title, newId);

                var blogEntity = new Blog
                {
                    Id = newId,
                    Title = request.Title,
                    Slug = slug,
                    ThumbnailUrl = request.ThumbnailUrl,
                    Content = request.Content,
                    ShortDescription = request.ShortDescription,
                    IsOfficial = true,
                    Status = BlogStatus.UnderAIReview,
                    ViewCount = 0,
                    CategoryId = request.CategoryId,
                    Tags = tags,
                    CreatedAt = DateTimeOffset.UtcNow,
                    AuthorId = request.CreatedBy
                };
 
                await _blogRepository.AddAsync(blogEntity);
                await _blogRepository.SaveChangesAsync(cancellationToken);
 
                _backgroundJobs.Enqueue<IBlogModerationBackgroundService>(
                    service => service.ModerateAdminBlogAsync(newId));

                return OperationResult<string>.Success(
                    blogEntity.Id,
                    201,
                    OperationMessages.CreateSuccess("Bài viết")
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo bài viết mới: {Message}", ex.Message);
                return OperationResult<string>.Failure(
                    AppErrors.ServerError,
                    500,
                    OperationMessages.CreateFail("Bài viết")
                );
            }
        }
    }
}