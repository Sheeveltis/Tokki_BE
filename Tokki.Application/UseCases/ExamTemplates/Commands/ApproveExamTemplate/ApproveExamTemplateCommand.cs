using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.ApproveExamTemplate
{
    public class ApproveExamTemplateCommand : IRequest<OperationResult<bool>>
    {
        public string ExamTemplateId { get; set; } = string.Empty;
    }
}