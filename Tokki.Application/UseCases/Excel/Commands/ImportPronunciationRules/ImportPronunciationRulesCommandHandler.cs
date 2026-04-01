using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Excel.Commands.ImportPronunciationRules
{
    public class ImportPronunciationRulesCommandHandler : IRequestHandler<ImportPronunciationRulesCommand, OperationResult<ImportRulesResponse>>
    {
        private readonly IExcelService _excelService;
        private readonly IPronunciationRuleRepository _ruleRepo;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ILogger<ImportPronunciationRulesCommandHandler> _logger;

        public ImportPronunciationRulesCommandHandler(
            IExcelService excelService,
            IPronunciationRuleRepository ruleRepo,
            IIdGeneratorService idGenerator,
            ILogger<ImportPronunciationRulesCommandHandler> logger)
        {
            _excelService = excelService;
            _ruleRepo = ruleRepo;
            _idGenerator = idGenerator;
            _logger = logger;
        }

        public async Task<OperationResult<ImportRulesResponse>> Handle(ImportPronunciationRulesCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bắt đầu ImportPronunciationRules. UserId: {UserId}, File: {FileName}",
                request.UserId, request.File.FileName);

            var response = new ImportRulesResponse();

            var extractedData = await _excelService.ExtractRuleDataAsync(request.File);

            if (extractedData == null || !extractedData.Any())
            {
                return OperationResult<ImportRulesResponse>.Failure(new Error("EXCEL_EMPTY", "Không tìm thấy dữ liệu hợp lệ trong file Excel."));
            }

            var newEntities = new List<Tokki.Domain.Entities.PronunciationRule>();

            foreach (var item in extractedData)
            {
                try
                {
                    var entity = new Tokki.Domain.Entities.PronunciationRule
                    {
                        PronunciationRuleId = _idGenerator.Generate(10),
                        RuleName = item.RuleName,
                        Description = item.Description,
                        Content = item.Content,
                        SortOrder = item.SortOrder,
                        IsDeleted = false,
                        CreateBy = request.UserId,
                        CreateDate = DateTime.UtcNow
                    };

                    newEntities.Add(entity);

                    response.SuccessList.Add(new RulePreviewDTO
                    {
                        RuleName = item.RuleName,
                        Reason = "Thành công"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xử lý dòng dữ liệu: {RuleName}", item.RuleName);
                    response.FailureList.Add(new RulePreviewDTO
                    {
                        RuleName = item.RuleName,
                        Reason = $"Lỗi: {ex.Message}"
                    });
                }
            }

            if (newEntities.Any())
            {
                try
                {
                    await _ruleRepo.AddRangeAsync(newEntities);
                    await _ruleRepo.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi lưu Database");
                    return OperationResult<ImportRulesResponse>.Failure(new Error("DATABASE_ERROR", "Lỗi lưu dữ liệu vào database."));
                }
            }

            var summaryMsg = $"Import hoàn tất. Thành công: {newEntities.Count}, Thất bại: {response.FailureList.Count}";
            return OperationResult<ImportRulesResponse>.Success(response, 200, summaryMsg);
        }
    }
}
