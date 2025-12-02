using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Blogs.Commands.UpdateBlog
{
    public class UpdateBlogCommandHandler : IRequestHandler<UpdateBlogCommand, OperationResult<bool>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly ILogger<UpdateBlogCommandHandler> _logger;

        public UpdateBlogCommandHandler(IBlogRepository blogRepository, ILogger<UpdateBlogCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(UpdateBlogCommand request, CancellationToken cancellationToken)
        {
            var blog = await _blogRepository.GetByIdAsync(request.Id);
            if (blog == null)
            {
                return OperationResult<bool>.Failure(AppErrors.BlogNotFound, 404, OperationMessages.NotFound("Bài viết"));
            }

            if (blog.CategoryId != request.CategoryId) 
            {
                var categoryExists = await _blogRepository.CategoryExistsAsync(request.CategoryId);
                if (!categoryExists)
                {
                    return OperationResult<bool>.Failure(AppErrors.CategoryNotFound, 404, OperationMessages.NotFound("Danh mục"));
                }
            }

            try
            {
                blog.Title = request.Title;
                blog.ThumbnailUrl = request.ThumbnailUrl;
                blog.Content = request.Content;
                blog.ShortDescription = request.ShortDescription;
                blog.Status = request.Status;
                blog.CategoryId = request.CategoryId;
                blog.UpdatedAt = DateTimeOffset.UtcNow; 

                if (!string.IsNullOrWhiteSpace(request.Slug))
                {
                    blog.Slug = SlugHelper.GenerateSlug(request.Slug, blog.Id);
                }
                else
                {
                    blog.Slug = SlugHelper.GenerateSlug(request.Title, blog.Id);
                }

                var newTags = await _blogRepository.GetOrCreateTagsAsync(request.Tags);

                blog.Tags.Clear();

                foreach (var tag in newTags)
                {
                    blog.Tags.Add(tag);
                }

                await _blogRepository.UpdateAsync(blog);
                await _blogRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(true, 200, OperationMessages.UpdateSuccess("Bài viết"));
            }
            catch (Exception ex)
            {
                var realError = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "Lỗi cập nhật Blog: {Error}", realError);
                return OperationResult<bool>.Failure(AppErrors.ServerError, 500, $"Lỗi hệ thống: {realError}");
            }
        }
    }
}
