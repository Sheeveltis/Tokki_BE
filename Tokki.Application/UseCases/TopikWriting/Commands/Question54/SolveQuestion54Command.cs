// Application/UseCases/TopikWriting/Question54/Commands/SolveQuestion54Command.cs
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.TopikWriting.Question54.DTOs;

namespace Tokki.Application.UseCases.TopikWriting.Question54.Commands
{
    public sealed class SolveQuestion54Command : IRequest<OperationResult<Question54ResultDto>>
    {
        public Question54RequestDto Payload { get; set; } = new();
    }
}