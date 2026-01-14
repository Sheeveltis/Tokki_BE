using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.SubmitExamTemplate
{
    public class SubmitExamTemplateCommand : IRequest<OperationResult<bool>>
    {
        public string ExamTemplateId { get; set; } = string.Empty;
    }
}