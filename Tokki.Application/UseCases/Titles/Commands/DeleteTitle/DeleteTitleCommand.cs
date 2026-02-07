using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Titles.Commands.DeleteTitle
{
    public record DeleteTitleCommand(string Id) : IRequest<OperationResult<string>>;
}