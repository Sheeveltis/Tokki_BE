using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.RejectExamTemplate
{
    public class RejectExamTemplateCommand : IRequest<OperationResult<bool>>
    {
        public string ExamTemplateId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}