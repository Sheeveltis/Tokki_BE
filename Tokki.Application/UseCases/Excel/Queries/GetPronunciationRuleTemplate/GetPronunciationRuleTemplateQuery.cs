using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Queries.GetPronunciationRuleTemplate
{
    public class GetPronunciationRuleTemplateQuery : IRequest<OperationResult<ExportFileDTO>>
    {
    }
}
