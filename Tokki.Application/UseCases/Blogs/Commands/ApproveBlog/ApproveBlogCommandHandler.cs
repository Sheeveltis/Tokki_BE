using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Helpers; // Import Helper
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Blogs.Commands.ApproveBlog
{
    public class ApproveBlogCommandHandler : IRequestHandler<ApproveBlogCommand, OperationResult<bool>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly EmailNotificationHelper _emailHelper; 
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApproveBlogCommandHandler> _logger;

        public ApproveBlogCommandHandler(
            IBlogRepository blogRepository,
            IAccountRepository accountRepository,
            EmailNotificationHelper emailHelper, 
            IHttpContextAccessor httpContextAccessor,
            ILogger<ApproveBlogCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _accountRepository = accountRepository;
            _emailHelper = emailHelper;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            ApproveBlogCommand request,
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
            if (blog.Status != BlogStatus.PendingApproval)
            {
                return OperationResult<bool>.Failure(
                    AppErrors.BlogInvalidPending,
                    400
                );
            }
            blog.Status = BlogStatus.Published;
            blog.UpdatedAt = DateTimeOffset.UtcNow;

            await _blogRepository.UpdateAsync(blog);

            if (!string.IsNullOrEmpty(blog.AuthorId))
            {
                var author = await _accountRepository.GetByIdAsync(blog.AuthorId);
                if (author != null && !string.IsNullOrEmpty(author.Email))
                {
                    await _emailHelper.SendContentApprovedAsync(
                        author.Email,
                        author.FullName,
                        blog.Title,
                        "Bài viết" 
                    );
                }
            }

            return OperationResult<bool>.Success(true, 200, OperationMessages.ApprovalSuccess("bài viết"));
        }
    }
}