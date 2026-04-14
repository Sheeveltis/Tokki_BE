using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Categories.Queries.ExportCategories
{
    public class ExportCategoriesHandler : IRequestHandler<ExportCategoriesQuery, OperationResult<byte[]>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IExcelService _excelService;

        public ExportCategoriesHandler(ICategoryRepository categoryRepository, IExcelService excelService)
        {
            _categoryRepository = categoryRepository;
            _excelService = excelService;
        }

        public async Task<OperationResult<byte[]>> Handle(ExportCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            
            var exportData = categories.Select(c => new CategoryExcelDTO
            {
                Name = c.Name,
                Slug = c.Slug
            }).ToList();

            var fileBytes = await _excelService.ExportCategoriesToExcelAsync(exportData, "Categories");

            return OperationResult<byte[]>.Success(fileBytes);
        }
    }
}
