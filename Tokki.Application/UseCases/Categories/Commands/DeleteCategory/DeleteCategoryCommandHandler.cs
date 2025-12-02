using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Categories.Commands.DeleteCategory
{
    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, OperationResult<bool>>
    {
        private readonly ICategoryRepository _repo;

        public DeleteCategoryCommandHandler(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _repo.GetByIdAsync(request.Id, cancellationToken);

            if (category == null)
            {
                return OperationResult<bool>.Failure(AppErrors.CategoryNotFound, 404, OperationMessages.NotFound("Danh mục"));
            }

            try
            {
                await _repo.DeleteAsync(category, cancellationToken);
                return OperationResult<bool>.Success(true, 200, OperationMessages.DeleteSuccess("Danh mục"));
            }
            catch (Exception)
            {
                return OperationResult<bool>.Failure(AppErrors.ServerError, 500, OperationMessages.DeleteFail("Danh mục"));
            }
        }
    }
}
