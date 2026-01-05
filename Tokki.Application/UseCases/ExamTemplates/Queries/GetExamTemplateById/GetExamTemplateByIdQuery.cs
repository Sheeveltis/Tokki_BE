using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.ExamTemplates.DTOs;

namespace Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplateById
{
    public class GetExamTemplateByIdQuery : IRequest<OperationResult<ExamTemplateDto>>
    {
        public string ExamTemplateId { get; set; } = string.Empty;
    }
}