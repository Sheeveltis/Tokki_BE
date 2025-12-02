using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, OperationResult<string>>
    {
        private readonly ICategoryRepository _repo;
        private readonly IIdGeneratorService _idGen;

        public CreateCategoryCommandHandler(ICategoryRepository repo, IIdGeneratorService idGen)
        {
            _repo = repo;
            _idGen = idGen;
        }

        public async Task<OperationResult<string>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return OperationResult<string>.Failure(AppErrors.ValidationFailed, 400, "Tên danh mục không được để trống.");
            }

            var newId = _idGen.Generate(10);

            var slug = SlugHelper.GenerateSlug(request.Name, newId);

            var category = new Category
            {
                Id = newId,
                Name = request.Name,
                Slug = slug,
                CreatedAt = DateTimeOffset.UtcNow
            };
            try
            {
                await _repo.AddAsync(category, cancellationToken);
                return OperationResult<string>.Success(newId, 201, OperationMessages.CreateSuccess("Danh mục"));
            }
            catch (Exception)
            {
                return OperationResult<string>.Failure(AppErrors.ServerError, 500, OperationMessages.CreateFail("Danh mục"));
            }
        }
    }
}
