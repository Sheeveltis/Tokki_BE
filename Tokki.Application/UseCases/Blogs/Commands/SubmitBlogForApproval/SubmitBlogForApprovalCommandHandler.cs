using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Blogs.Commands.SubmitBlogForApproval
{
    public class SubmitBlogForApprovalCommandHandler
         : IRequestHandler<SubmitBlogForApprovalCommand, OperationResult<bool>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SubmitBlogForApprovalCommandHandler> _logger;

        public SubmitBlogForApprovalCommandHandler(
            IBlogRepository blogRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SubmitBlogForApprovalCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _httpContextAccessor = httpContextAccessor;
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

            if (blog.AuthorId != currentUserId)
            {
                return OperationResult<bool>.Failure(
                    AppErrors.IsNotAuthor,
                    403
                );
            }

            if (blog.Status != BlogStatus.Draft && blog.Status != BlogStatus.Rejected)
            {
                return OperationResult<bool>.Failure(
                   AppErrors.BlogInvalidPending,
                    400
                );
            }

            blog.Status = BlogStatus.PendingApproval;
            blog.UpdatedAt = DateTimeOffset.UtcNow;

            await _blogRepository.UpdateAsync(blog);
            await _blogRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(
                true,
                200,
                OperationMessages.PendingSuccess("bài viết")
            );
        }
    }
}
