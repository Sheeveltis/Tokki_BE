using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Categories.Commands.ImportCategories
{
    public class ImportCategoriesHandler : IRequestHandler<ImportCategoriesCommand, OperationResult<bool>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IExcelService _excelService;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<ImportCategoriesHandler> _logger;

        public ImportCategoriesHandler(
            ICategoryRepository categoryRepository,
            IExcelService excelService,
            IIdGeneratorService idGeneratorService,
            ILogger<ImportCategoriesHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _excelService = excelService;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(ImportCategoriesCommand request, CancellationToken cancellationToken)
        {
            // 1. Phân tích dữ liệu từ Excel
            var categoryData = await _excelService.ExtractCategoryDataAsync(request.File);
            if (categoryData == null || !categoryData.Any())
            {
                return OperationResult<bool>.Failure("File Excel không có dữ liệu danh mục hợp lệ.", 400);
            }

            // 2. Bắt đầu Transaction - All or Nothing
            await using var transaction = await _categoryRepository.BeginTransactionAsync(cancellationToken);

            try
            {
                // 3. Lấy danh sách hiện có để check trùng
                var existingCategories = await _categoryRepository.GetAllAsync(cancellationToken);
                var existingNames = existingCategories.Select(c => c.Name.ToLowerInvariant().Trim()).ToHashSet();

                var newCategories = new List<Category>();
                var duplicateInFile = new HashSet<string>();

                foreach (var data in categoryData)
                {
                    if (string.IsNullOrWhiteSpace(data.Name)) continue;
                    
                    var normalizedName = data.Name.ToLowerInvariant().Trim();

                    // Kiểm tra trùng lặp với Database
                    if (existingNames.Contains(normalizedName))
                    {
                        _logger.LogInformation("Bỏ qua danh mục đã tồn tại trong DB: {Name}", data.Name);
                        continue;
                    }

                    // Kiểm tra trùng lặp ngay trong chính file Excel (nếu có 2 dòng giống nhau)
                    if (duplicateInFile.Contains(normalizedName))
                    {
                        _logger.LogInformation("Bỏ qua danh mục trùng lặp trong file: {Name}", data.Name);
                        continue;
                    }

                    string newId = _idGeneratorService.GenerateCustom(10);
                    string slug = !string.IsNullOrWhiteSpace(data.Slug) 
                        ? data.Slug 
                        : SlugHelper.GenerateSlug(data.Name, newId);

                    newCategories.Add(new Category
                    {
                        Id = newId,
                        Name = data.Name.Trim(),
                        Slug = slug,
                        CreatedAt = DateTimeOffset.UtcNow
                    });

                    duplicateInFile.Add(normalizedName);
                }

                if (newCategories.Any())
                {
                    // Lưu hàng loạt
                    await _categoryRepository.AddRangeAsync(newCategories, cancellationToken);
                }

                // 4. Commit Transaction
                await transaction.CommitAsync(cancellationToken);

                return OperationResult<bool>.Success(true, 200, $"Đã nhập thành công {newCategories.Count} danh mục mới. Các danh mục trùng lặp đã được tự động bỏ qua.");
            }
            catch (Exception ex)
            {
                // 5. Tự động Rollback nếu có lỗi xảy ra
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi khi import category, đã rollback transaction.");
                return OperationResult<bool>.Failure($"Lỗi hệ thống khi nhập liệu: {ex.Message}", 500);
            }
        }
    }
}
