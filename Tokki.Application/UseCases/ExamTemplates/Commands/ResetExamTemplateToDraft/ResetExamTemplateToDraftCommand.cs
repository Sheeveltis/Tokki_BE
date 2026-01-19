using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.ResetExamTemplateToDraft
{
    public class ResetExamTemplateToDraftCommand : IRequest<OperationResult<bool>>
    {
        public string ExamTemplateId { get; set; } = string.Empty;
    }
}