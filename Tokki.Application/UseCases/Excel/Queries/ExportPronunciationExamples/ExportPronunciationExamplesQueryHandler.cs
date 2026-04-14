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
        private readonly IExcelService _excelService;
 
        public ExportPronunciationExamplesQueryHandler(IPronunciationExampleRepository exampleRepo, IExcelService excelService)
        {
            _exampleRepo = exampleRepo;
            _excelService = excelService;
        }
 
        public async Task<OperationResult<ExportFileDTO>> Handle(ExportPronunciationExamplesQuery request, CancellationToken cancellationToken)
        {
            var examples = await _exampleRepo.GetAllAsync();
 
            var exportData = examples.Select(e => new PronunciationExampleExcelDTO
            {
                PronunciationRuleId = e.PronunciationRuleId,
                TargetScript = e.TargetScript,
                RawScript = e.RawScript,
                PhoneticScript = e.PhoneticScript,
                Meaning = e.Meaning,
                SortOrder = e.SortOrder
            }).ToList();
 
            var excelBytes = await _excelService.ExportExamplesToExcelAsync(exportData, "PronunciationExamples");
 
            var response = new ExportFileDTO
            {
                FileContent = excelBytes,
                FileName = $"Tokki_PronunciationExample_{DateTime.Now:ddMMyyyy}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
 
            return OperationResult<ExportFileDTO>.Success(response);
        }
    }
 }
