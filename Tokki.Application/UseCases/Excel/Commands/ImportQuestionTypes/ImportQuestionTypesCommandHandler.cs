using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Excel.Commands.ImportQuestionTypes
{
    public class ImportQuestionTypesCommandHandler : IRequestHandler<ImportQuestionTypesCommand, OperationResult<ImportQuestionTypeResponse>>
    {
        private readonly IExcelBaseService _excelBaseService;
        private readonly IQuestionTypeRepository _questionTypeRepository;
        private readonly IIdGeneratorService _idGeneratorService;

        public ImportQuestionTypesCommandHandler(
            IExcelBaseService excelBaseService,
            IQuestionTypeRepository questionTypeRepository,
            IIdGeneratorService idGeneratorService)
        {
            _excelBaseService = excelBaseService;
            _questionTypeRepository = questionTypeRepository;
            _idGeneratorService = idGeneratorService;
        }

        public async Task<OperationResult<ImportQuestionTypeResponse>> Handle(ImportQuestionTypesCommand request, CancellationToken cancellationToken)
        {
            var response = new ImportQuestionTypeResponse();

            var excelResult = await _excelBaseService.ImportAsync<QuestionTypeExcelDTO>(request.File, null, cancellationToken);

            if (excelResult.Errors.Any())
            {
                foreach (var err in excelResult.Errors)
                {
                    response.FailureList.Add(new QuestionTypePreviewDTO
                    {
                        Code = "Lỗi định dạng",
                        Name = $"Dòng {err.RowIndex}",
                        Reason = err.Reason
                    });
                }
            }

            var codesInExcel = excelResult.SuccessItems
                .Select(x => x.Data.Code?.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            var existingCodesInDb = await _questionTypeRepository.GetExistingCodesAsync(codesInExcel, cancellationToken);
            var existingDbCodesSet = new HashSet<string>(existingCodesInDb, StringComparer.OrdinalIgnoreCase);
            var processedCodesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var newQuestionTypes = new List<QuestionType>();

            int currentMaxOrderIndex = await _questionTypeRepository.GetMaxOrderIndexAsync(cancellationToken);

            foreach (var successDetail in excelResult.SuccessItems)
            {
                var item = successDetail.Data;
                int rowIndex = successDetail.RowIndex;

                if (string.IsNullOrWhiteSpace(item.Code) || string.IsNullOrWhiteSpace(item.Name))
                {
                    response.FailureList.Add(new QuestionTypePreviewDTO
                    {
                        Code = item.Code ?? "N/A",
                        Name = $"Dòng {rowIndex}: {item.Name ?? "N/A"}",
                        Reason = "Thiếu dữ liệu bắt buộc (Code hoặc Tên)."
                    });
                    continue;
                }

                string currentCode = item.Code.Trim();

                if (!processedCodesInFile.Add(currentCode))
                {
                    response.FailureList.Add(new QuestionTypePreviewDTO
                    {
                        Code = currentCode,
                        Name = item.Name,
                        Reason = $"Dòng {rowIndex}: Mã Code bị trùng lặp trong file."
                    });
                    continue;
                }

                if (existingDbCodesSet.Contains(currentCode))
                {
                    response.FailureList.Add(new QuestionTypePreviewDTO
                    {
                        Code = currentCode,
                        Name = item.Name,
                        Reason = $"Dòng {rowIndex}: Mã Code đã tồn tại trong hệ thống."
                    });
                    continue;
                }

                try
                {
                    currentMaxOrderIndex++;

                    var entity = new QuestionType
                    {
                        QuestionTypeId = _idGeneratorService.Generate(10),
                        Code = currentCode,
                        Name = item.Name.Trim(),
                        Description = item.Description?.Trim(),
                        ExamType = Enum.Parse<ExamType>(item.ExamType, true),
                        Skill = Enum.Parse<QuestionSkill>(item.Skill, true),
                        Difficulty = Enum.Parse<DifficultyLevel>(item.Difficulty, true),
                        IsActive = true,
                        OrderIndex = currentMaxOrderIndex,
                    };

                    newQuestionTypes.Add(entity);

                    response.SuccessList.Add(new QuestionTypePreviewDTO
                    {
                        Code = currentCode,
                        Name = item.Name,
                        Reason = "Hợp lệ"
                    });
                }
                catch (Exception)
                {
                    response.FailureList.Add(new QuestionTypePreviewDTO
                    {
                        Code = currentCode,
                        Name = item.Name,
                        Reason = $"Dòng {rowIndex}: Sai định dạng format."
                    });
                }
            }

            if (newQuestionTypes.Any())
            {
                try
                {
                    await _questionTypeRepository.AddRangeAsync(newQuestionTypes);
                    await _questionTypeRepository.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    return OperationResult<ImportQuestionTypeResponse>.Failure(new Error("DB_ERROR", "Lỗi lưu database: " + ex.Message));
                }
            }

            var summaryMsg = $"Import hoàn tất. Thành công: {newQuestionTypes.Count}, Thất bại: {response.FailureList.Count}";
            return OperationResult<ImportQuestionTypeResponse>.Success(response, 200, summaryMsg);
        }
    }
}