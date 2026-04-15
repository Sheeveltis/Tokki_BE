using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;
using Hangfire;
using Tokki.Application.IServices;

namespace Tokki.Application.UseCases.Blogs.Commands.SubmitBlogForApproval
{
    public class SubmitBlogForApprovalCommandHandler
         : IRequestHandler<SubmitBlogForApprovalCommand, OperationResult<bool>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly ILogger<SubmitBlogForApprovalCommandHandler> _logger;

        public SubmitBlogForApprovalCommandHandler(
            IBlogRepository blogRepository,
            IHttpContextAccessor httpContextAccessor,
            IBackgroundJobClient backgroundJobs,
            ILogger<SubmitBlogForApprovalCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _httpContextAccessor = httpContextAccessor;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            SubmitBlogForApprovalCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return OperationResult<bool>.Failure(AppErrors.UserUnauthorized);
            }

            var blog = await _blogRepository.GetByIdAsync(request.BlogId);
            if (blog == null)
            {
                return OperationResult<bool>.Failure(
                    AppErrors.BlogNotFound,
                    404
                );
            }

            // Kiểm tra quyền: Chỉ tác giả mới được gửi duyệt
            if (blog.AuthorId != currentUserId)
            {
                return OperationResult<bool>.Failure(
                    AppErrors.IsNotAuthor,
                    403
                );
            }

            // Chỉ cho phép gửi duyệt khi đang là nháp hoặc đã bị từ chối
            var allowedStatuses = new[] { 
                BlogStatus.Draft, 
                BlogStatus.Rejected, 
                BlogStatus.AIRejected, 
                BlogStatus.AIReviewFailed 
            };

            if (!allowedStatuses.Contains(blog.Status))
            {
                return OperationResult<bool>.Failure(
                    new Error("Blog.InvalidStatus", "Bài viết không ở trạng thái có thể gửi duyệt."),
                    400
                );
            }

            // Chuyển trạng thái sang AI đang kiểm duyệt
            blog.Status = BlogStatus.UnderAIReview;
            blog.UpdatedAt = DateTimeOffset.UtcNow;

            await _blogRepository.UpdateAsync(blog);
            await _blogRepository.SaveChangesAsync(cancellationToken);

            // Đưa vào hàng đợi Hangfire để AI duyệt ngầm
            _backgroundJobs.Enqueue<IBlogModerationBackgroundService>(
                service => service.ModerateBlogAsync(blog.Id));

            return OperationResult<bool>.Success(
                true,
                200,
                "Bài viết đã được gửi và đang trong quá trình kiểm duyệt tự động."
            );
        }
    }
}

