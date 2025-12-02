using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, OperationResult<bool>>
    {
        private readonly ICategoryRepository _repo;

        public UpdateCategoryCommandHandler(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<bool>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _repo.GetByIdAsync(request.Id, cancellationToken);

            if (category == null)
            {
                return OperationResult<bool>.Failure(AppErrors.CategoryNotFound, 404, OperationMessages.NotFound("Danh mục"));
            }

            category.Name = request.Name;
            category.Slug = SlugHelper.GenerateSlug(request.Name, category.Id);

            try
            {
                await _repo.UpdateAsync(category, cancellationToken);
                return OperationResult<bool>.Success(true, 200, OperationMessages.UpdateSuccess("Danh mục"));
            }
            catch (Exception)
            {
                return OperationResult<bool>.Failure(AppErrors.ServerError, 500, OperationMessages.UpdateFail("Danh mục"));
            }
        }
    }
}
