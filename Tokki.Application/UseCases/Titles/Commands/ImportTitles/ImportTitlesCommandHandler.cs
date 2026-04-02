using MediatR;
using System.Text.RegularExpressions;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Titles.Commands.ImportTitles
{
    public class ImportTitlesCommandHandler : IRequestHandler<ImportTitlesCommand, OperationResult<int>>
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IExcelService _excelService;
        private readonly IIdGeneratorService _idGenerator;

        private static readonly Regex HexColorRegex = new Regex("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");

        public ImportTitlesCommandHandler(ITitleRepository titleRepository, IExcelService excelService, IIdGeneratorService idGenerator)
        {
            _titleRepository = titleRepository;
            _excelService = excelService;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<int>> Handle(ImportTitlesCommand request, CancellationToken cancellationToken)
        {
            if (request.File == null || request.File.Length == 0)
                return OperationResult<int>.Failure("File không hợp lệ.");

            var excelData = await _excelService.ExtractTitleDataAsync(request.File);
            if (excelData == null || !excelData.Any())
                return OperationResult<int>.Failure("Không có dữ liệu trong file.");

            var errors = new List<string>();
            var validTitles = new List<Title>();

            for (int i = 0; i < excelData.Count; i++)
            {
                var dto = excelData[i];
                int rowIndex = i + 2; 

                // 1. Kiểm tra Null/Empty
                if (string.IsNullOrWhiteSpace(dto.Name)) errors.Add($"Dòng {rowIndex}: Tên danh hiệu không được để trống.");
                if (string.IsNullOrWhiteSpace(dto.Description)) errors.Add($"Dòng {rowIndex}: Mô tả không được để trống.");
                if (string.IsNullOrWhiteSpace(dto.ColorHex)) errors.Add($"Dòng {rowIndex}: Mã màu HEX không được để trống.");
                if (string.IsNullOrWhiteSpace(dto.IconUrl)) errors.Add($"Dòng {rowIndex}: URL Icon không được để trống.");

                // 2. Kiểm tra định dạng màu HEX
                if (!string.IsNullOrWhiteSpace(dto.ColorHex) && !HexColorRegex.IsMatch(dto.ColorHex.Trim()))
                    errors.Add($"Dòng {rowIndex}: Mã màu '{dto.ColorHex}' không đúng định dạng HEX (VD: #FFFFFF).");

                // 3. Kiểm tra Enum Loại điều kiện
                if (!Enum.TryParse<TitleRequirementType>(dto.RequirementType, true, out var type))
                {
                    errors.Add($"Dòng {rowIndex}: Loại điều kiện '{dto.RequirementType}' không hợp lệ (Hỗ trợ: Level, XP, Streak, InactivityDays, StudyDaysTotal, SystemGiven).");
                }

                // 4. Kiểm tra Quantity
                if (dto.RequirementQuantity < 0)
                {
                    errors.Add($"Dòng {rowIndex}: Giá trị điều kiện không được âm.");
                }

                if (errors.Any()) continue;

                // 5. Kiểm tra trùng tên trong Database (chỉ check với Active)
                var existingInDb = await _titleRepository.GetTitleByNameAsync(dto.Name.Trim(), TitleStatus.Active);
                if (existingInDb != null)
                {
                    errors.Add($"Dòng {rowIndex}: Tên '{dto.Name}' đã tồn tại trong hệ thống dưới dạng Active.");
                }

                // 6. Kiểm tra trùng tên ngay trong File Import
                if (validTitles.Any(x => x.Name.Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add($"Dòng {rowIndex}: Tên '{dto.Name}' bị lặp lại trong chính file import.");
                }

                if (errors.Any()) continue;

                // 7. Map dữ liệu
                validTitles.Add(new Title
                {
                    TitleId = _idGenerator.GenerateCustom(10),
                    Name = dto.Name.Trim(),
                    Description = dto.Description?.Trim(),
                    ColorHex = dto.ColorHex.Trim(),
                    IconUrl = dto.IconUrl.Trim(),
                    RequirementType = type,
                    RequirementQuantity = dto.RequirementQuantity,
                    Status = dto.Status?.Equals("Inactive", StringComparison.OrdinalIgnoreCase) == true ? TitleStatus.Inactive : TitleStatus.Active
                });
            }

            // Nếu có bất kỳ lỗi nào, không import cái nào cả
            if (errors.Any())
            {
                string errorSummary = "Tệp Excel có các lỗi sau:\n" + string.Join("\n", errors.Take(30)); // Show up to 30 errors
                return OperationResult<int>.Failure(errorSummary, 400);
            }

            // 8. Thực hiện Import nguyên khối (Atomic)
            int count = 0;
            foreach (var title in validTitles)
            {
                await _titleRepository.AddAsync(title);
                count++;
            }

            return OperationResult<int>.Success(count, 201, $"Nhập thành công {count} danh hiệu.");
        }
    }
}
