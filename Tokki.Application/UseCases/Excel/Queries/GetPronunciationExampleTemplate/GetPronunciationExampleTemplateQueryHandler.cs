using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Queries.GetPronunciationExampleTemplate
{
    public class GetPronunciationExampleTemplateQueryHandler : IRequestHandler<GetPronunciationExampleTemplateQuery, OperationResult<ExportFileDTO>>
    {
        private readonly IExcelService _excelService;

        public GetPronunciationExampleTemplateQueryHandler(IExcelService excelService)
        {
            _excelService = excelService;
        }

        public async Task<OperationResult<ExportFileDTO>> Handle(GetPronunciationExampleTemplateQuery request, CancellationToken cancellationToken)
        {
            var excelBytes = await _excelService.GetPronunciationExampleTemplateAsync();

            var response = new ExportFileDTO
            {
                FileContent = excelBytes,
                FileName = "Tokki_PronunciationExample_Template.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };

            return OperationResult<ExportFileDTO>.Success(response);
        }
    }
}
