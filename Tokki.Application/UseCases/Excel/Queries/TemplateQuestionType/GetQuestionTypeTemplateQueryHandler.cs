using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Queries.TemplateQuestionType
{
    public class GetQuestionTypeTemplateQueryHandler : IRequestHandler<GetQuestionTypeTemplateQuery, OperationResult<(byte[] FileBytes, string FileName)>>
    {
        private readonly IExcelBaseService _excelBaseService;

        public GetQuestionTypeTemplateQueryHandler(IExcelBaseService excelBaseService)
        {
            _excelBaseService = excelBaseService;
        }

        public async Task<OperationResult<(byte[] FileBytes, string FileName)>> Handle(GetQuestionTypeTemplateQuery request, CancellationToken cancellationToken)
        {
            try
            {
                byte[] fileBytes = await _excelBaseService.GenerateTemplateAsync<QuestionTypeExcelDTO>("Template_QuestionType");

                string fileName = "Template_Import_QuestionType.xlsx";

                return OperationResult<(byte[], string)>.Success((fileBytes, fileName), 200, "Tạo template Question Type thành công.");
            }
            catch (Exception ex)
            {
                return OperationResult<(byte[], string)>.Failure(new Error("TEMPLATE_ERROR", ex.Message));
            }
        }
    }
}
