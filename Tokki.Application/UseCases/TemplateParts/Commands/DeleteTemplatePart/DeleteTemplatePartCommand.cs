using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.TemplateParts.Commands.DeleteTemplatePart
{
    public record DeleteTemplatePartCommand(string TemplatePartId) : IRequest<OperationResult<string>>;
}