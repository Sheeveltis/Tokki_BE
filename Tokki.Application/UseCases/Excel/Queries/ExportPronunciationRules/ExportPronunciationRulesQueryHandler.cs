using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Queries.ExportPronunciationRules
{
    public class ExportPronunciationRulesQueryHandler : IRequestHandler<ExportPronunciationRulesQuery, OperationResult<ExportFileDTO>>
    {
        private readonly IPronunciationRuleRepository _ruleRepo;
        private readonly IExcelService _excelService;

        public ExportPronunciationRulesQueryHandler(IPronunciationRuleRepository ruleRepo, IExcelService excelService)
        {
            _ruleRepo = ruleRepo;
            _excelService = excelService;
        }

        public async Task<OperationResult<ExportFileDTO>> Handle(ExportPronunciationRulesQuery request, CancellationToken cancellationToken)
        {
            // Use fully qualified name to avoid namespace conflict with Tokki.Application.UseCases.PronunciationRule
            var rules = await _ruleRepo.GetAllActiveRulesAsync(cancellationToken);

            var exportData = rules.Select(r => new PronunciationRuleExcelDTO
            {
                RuleName = r.RuleName,
                Description = r.Description,
                Content = r.Content,
                SortOrder = r.SortOrder
            }).ToList();

            var excelBytes = await _excelService.ExportRulesToExcelAsync(exportData, "PronunciationRules");

            var response = new ExportFileDTO
            {
                FileContent = excelBytes,
                FileName = $"PronunciationRules_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };

            return OperationResult<ExportFileDTO>.Success(response);
        }
    }
}
