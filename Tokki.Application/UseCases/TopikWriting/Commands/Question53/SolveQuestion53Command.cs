// Application/UseCases/TopikWriting/Question53/Commands/SolveQuestion53Command.cs
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.TopikWriting.Question53.DTOs;

namespace Tokki.Application.UseCases.TopikWriting.Question53.Commands
{
    public sealed class SolveQuestion53Command : IRequest<OperationResult<Question53ResultDto>>
    {
        public Question53RequestDto Payload { get; set; } = new();
    }
}