using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Titles.Commands.DeleteTitle
{
    public record DeleteTitleCommand(int Id) : IRequest<OperationResult<string>>;
}