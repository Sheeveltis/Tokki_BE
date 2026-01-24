using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Blogs.Commands.IncreaseViewCount
{
    public class IncreaseViewCountCommandHandler : IRequestHandler<IncreaseViewCountCommand, OperationResult<bool>>
    {
        private readonly IBlogRepository _blogRepository;

        public IncreaseViewCountCommandHandler(IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
        }

        public async Task<OperationResult<bool>> Handle(
            IncreaseViewCountCommand request,
            CancellationToken cancellationToken)
        {
            var isSuccess = await _blogRepository.IncreaseViewCountAsync(request.BlogId);

            if (!isSuccess)
            {
                return OperationResult<bool>.Failure(
                    AppErrors.BlogNotFound,
                    404
                );
            }

            return OperationResult<bool>.Success(true, 200, "Tăng view thành công.");
        }
    }
}
