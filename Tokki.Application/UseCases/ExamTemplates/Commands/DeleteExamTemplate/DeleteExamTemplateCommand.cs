using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.DeleteExamTemplate
{
    public record DeleteExamTemplateCommand(string ExamTemplateId) : IRequest<OperationResult<string>>;
}