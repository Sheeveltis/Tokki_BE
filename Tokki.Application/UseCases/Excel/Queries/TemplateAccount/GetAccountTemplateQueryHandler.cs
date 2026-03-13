using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Queries.GetTemplate
{
    public class GetAccountTemplateQueryHandler : IRequestHandler<GetAccountTemplateQuery, OperationResult<(byte[] FileBytes, string FileName)>>
    {
        private readonly IExcelBaseService _excelBaseService;

        public GetAccountTemplateQueryHandler(IExcelBaseService excelBaseService)
        {
            _excelBaseService = excelBaseService;
        }

        public async Task<OperationResult<(byte[] FileBytes, string FileName)>> Handle(GetAccountTemplateQuery request, CancellationToken cancellationToken)
        {
            try
            {
                byte[] fileBytes = await _excelBaseService.GenerateTemplateAsync<AccountExcelDTO>("Template_Account");

                string fileName = "Template_Import_Account.xlsx";

                return OperationResult<(byte[], string)>.Success((fileBytes, fileName), 200, "Tạo template thành công.");
            }
            catch (Exception ex)
            {
                return OperationResult<(byte[], string)>.Failure(new Error("TEMPLATE_ERROR", ex.Message));
            }
        }
    }
}
