using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;
 
namespace Tokki.Application.UseCases.Excel.Queries.ExportPronunciationExamples
 {
    public class ExportPronunciationExamplesQueryHandler : IRequestHandler<ExportPronunciationExamplesQuery, OperationResult<ExportFileDTO>>
    {
        private readonly IPronunciationExampleRepository _exampleRepo;
        private readonly IPronunciationRuleRepository _ruleRepo;
        private readonly IExcelService _excelService;

        public ExportPronunciationExamplesQueryHandler(
            IPronunciationExampleRepository exampleRepo, 
            IPronunciationRuleRepository ruleRepo, 
            IExcelService excelService)
        {
            _exampleRepo = exampleRepo;
            _ruleRepo = ruleRepo;
            _excelService = excelService;
        }
 
        public async Task<OperationResult<ExportFileDTO>> Handle(ExportPronunciationExamplesQuery request, CancellationToken cancellationToken)
        {
            List<Tokki.Domain.Entities.PronunciationExample> examples;
            string ruleNameSuffix = "";
            
            if (!string.IsNullOrEmpty(request.PronunciationRuleId))
            {
                var rule = await _ruleRepo.GetByIdAsync(request.PronunciationRuleId);
                ruleNameSuffix = rule != null ? $"_{rule.RuleName.Replace(" ", "_")}" : "";
                examples = await _exampleRepo.GetExamplesByRuleIdAsync(request.PronunciationRuleId, cancellationToken);
            }
            else
            {
                examples = await _exampleRepo.GetAllAsync(cancellationToken);
            }

            // Sắp xếp theo SortOrder trước khi export
            var exportData = examples.OrderBy(x => x.SortOrder).Select(e => new PronunciationExampleExcelDTO
            {
                PronunciationRuleId = e.PronunciationRuleId,
                TargetScript = e.TargetScript,
                RawScript = e.RawScript,
                PhoneticScript = e.PhoneticScript,
                Meaning = e.Meaning,
                SortOrder = e.SortOrder,
                Difficulty = e.Difficulty.ToString()
            }).ToList();
 
            var excelBytes = await _excelService.ExportExamplesToExcelAsync(exportData, "PronunciationExamples");
 
            var response = new ExportFileDTO
            {
                FileContent = excelBytes,
                FileName = $"Tokki{ruleNameSuffix}_{DateTime.Now:ddMMyyyy}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
 
            return OperationResult<ExportFileDTO>.Success(response);
        }
    }
 }
