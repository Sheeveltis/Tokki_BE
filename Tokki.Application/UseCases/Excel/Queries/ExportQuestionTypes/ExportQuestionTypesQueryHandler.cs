using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Queries.ExportQuestionTypes
{
    public class ExportQuestionTypesQueryHandler : IRequestHandler<ExportQuestionTypesQuery, OperationResult<(byte[] FileBytes, string FileName)>>
    {
        private readonly IExcelBaseService _excelBaseService;
        private readonly IQuestionTypeRepository _questionTypeRepository;

        public ExportQuestionTypesQueryHandler(IExcelBaseService excelBaseService, IQuestionTypeRepository questionTypeRepository)
        {
            _excelBaseService = excelBaseService;
            _questionTypeRepository = questionTypeRepository;
        }

        public async Task<OperationResult<(byte[] FileBytes, string FileName)>> Handle(ExportQuestionTypesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var data = await _questionTypeRepository.GetAllAsync(cancellationToken);

                var exportData = data.Select(q => new QuestionTypeExcelDTO
                {
                    Code = q.Code,
                    Name = q.Name,
                    Description = q.Description,
                    ExamType = q.ExamType.ToString(),
                    Skill = q.Skill.ToString(),
                    Difficulty = q.Difficulty.ToString()
                }).ToList();

                byte[] fileBytes = await _excelBaseService.ExportAsync(exportData, "QuestionTypes");
                string fileName = $"QuestionTypes_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                return OperationResult<(byte[], string)>.Success((fileBytes, fileName), 200, "Xuất file thành công.");
            }
            catch (Exception ex)
            {
                return OperationResult<(byte[], string)>.Failure(new Error("EXPORT_ERROR", ex.Message));
            }
        }
    }
}