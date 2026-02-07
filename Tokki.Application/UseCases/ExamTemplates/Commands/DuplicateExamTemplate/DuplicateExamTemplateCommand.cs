using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.DuplicateExamTemplate
{
    public record DuplicateExamTemplateCommand(string ExamTemplateId) : IRequest<OperationResult<string>>;
}