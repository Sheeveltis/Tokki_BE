using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Blogs.Commands.DeleteBlog
{
    public class DeleteBlogCommandHandler : IRequestHandler<DeleteBlogCommand, OperationResult<bool>>
    {
        private readonly IBlogRepository _blogRepository;

        public DeleteBlogCommandHandler(IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
        }

        public async Task<OperationResult<bool>> Handle(DeleteBlogCommand request, CancellationToken cancellationToken)
        {
            var blog = await _blogRepository.GetByIdAsync(request.Id);

            if (blog == null)
            {
                return OperationResult<bool>.Failure(AppErrors.BlogNotFound, 404, OperationMessages.NotFound("Bài viết"));
            }


            try
            {
                await _blogRepository.DeleteAsync(blog);
                await _blogRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(true, 200, OperationMessages.DeleteSuccess("Bài viết"));
            }
            catch (Exception)
            {
                return OperationResult<bool>.Failure(AppErrors.ServerError, 500, OperationMessages.DeleteFail("Bài viết"));
            }
        }
    }
}
