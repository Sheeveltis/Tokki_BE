using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Hangfire;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Blogs.Commands.CreateUserBlog
{
    public class CreateUserBlogCommandHandler : IRequestHandler<CreateUserBlogCommand, OperationResult<string>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly AppNotificationHelper _notificationHelper;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<CreateUserBlogCommandHandler> _logger;

        public CreateUserBlogCommandHandler(
            IBlogRepository blogRepository,
            IAccountRepository accountRepository,
            ISystemConfigRepository systemConfigRepository,
            IBackgroundJobClient backgroundJobs,
            AppNotificationHelper notificationHelper,
            IIdGeneratorService idGeneratorService,
            ILogger<CreateUserBlogCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _accountRepository = accountRepository;
            _systemConfigRepository = systemConfigRepository;
            _backgroundJobs = backgroundJobs;
            _notificationHelper = notificationHelper;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(CreateUserBlogCommand request, CancellationToken cancellationToken)
        {
            // 1. Check user level requirement
            var minLevelConfig = await _systemConfigRepository.GetByKeyAsync("MIN_LEVEL_POST_BLOG");
            if (minLevelConfig != null && int.TryParse(minLevelConfig.Value, out int requiredLevel))
            {
                var user = await _accountRepository.GetByIdAsync(request.CreatedBy);
                if (user == null)
                {
                    return OperationResult<string>.Failure(
                        AppErrors.UserNotFound,
                        404,
                        OperationMessages.NotFound("Người dùng")
                    );
                }

                int userLevel = LevelEngine.GetLevel(user.TotalXP);
                if (userLevel < requiredLevel)
                {
                    return OperationResult<string>.Failure(
                        new Error("Blog.InsufficientLevel", "Không đủ điều kiện đăng bài."),
                        403,
                        $"Bạn cần đạt Level {requiredLevel} để đăng bài viết."
                    );
                }
            }

            // 2. Tách logic duyệt A.I ra Background Job (Hangfire) - Không chặn luồng chính
            // Logic này sẽ được thực hiện bất đồng bộ bởi BlogModerationBackgroundService


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
                var tags = await _blogRepository.GetOrCreateTagsAsync(request.Tags);
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
                    IsOfficial = false, // Bài viết từ cộng đồng
                    Status = BlogStatus.UnderAIReview, // Đang chờ AI duyệt
                    ViewCount = 0,
                    CategoryId = request.CategoryId,
                    Tags = tags,
                    CreatedAt = DateTimeOffset.UtcNow,
                    AuthorId = request.CreatedBy
                };

                await _blogRepository.AddAsync(blogEntity);
                await _blogRepository.SaveChangesAsync(cancellationToken);

                // 3. Đưa vào hàng đợi Hangfire để AI duyệt ngầm
                _backgroundJobs.Enqueue<IBlogModerationBackgroundService>(
                    service => service.ModerateBlogAsync(newId));

                // 4. Gửi thông báo "Đã nhận bài" qua Helper
                await _notificationHelper.SendBlogSubmissionReceivedAsync(
                    request.CreatedBy,
                    request.Title,
                    newId
                );

                return OperationResult<string>.Success(
                    blogEntity.Id,
                    201,
                    "Bài viết đã được gửi và đang trong quá trình kiểm duyệt tự động."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo bài viết mới từ người dùng: {Message}", ex.Message);
                return OperationResult<string>.Failure(
                    AppErrors.ServerError,
                    500,
                    OperationMessages.CreateFail("Bài viết")
                );
            }
        }
    }
}
