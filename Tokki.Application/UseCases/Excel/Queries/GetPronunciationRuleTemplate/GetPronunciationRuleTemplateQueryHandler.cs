using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Queries.GetPronunciationRuleTemplate
{
    public class GetPronunciationRuleTemplateQueryHandler : IRequestHandler<GetPronunciationRuleTemplateQuery, OperationResult<ExportFileDTO>>
    {
        private readonly IExcelService _excelService;

        public GetPronunciationRuleTemplateQueryHandler(IExcelService excelService)
        {
            _excelService = excelService;
        }

        public async Task<OperationResult<ExportFileDTO>> Handle(GetPronunciationRuleTemplateQuery request, CancellationToken cancellationToken)
        {
            var excelBytes = await _excelService.GetPronunciationRuleTemplateAsync();

            var response = new ExportFileDTO
            {
                FileContent = excelBytes,
                FileName = "Tokki_PronunciationRule_Template.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };

            return OperationResult<ExportFileDTO>.Success(response);
        }
    }
}
