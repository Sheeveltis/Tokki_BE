// Application/UseCases/TopikWriting/Question52/Commands/SolveQuestion52Command.cs
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.TopikWriting.Question52.DTOs;

namespace Tokki.Application.UseCases.TopikWriting.Question52.Commands
{
    public sealed class SolveQuestion52Command : IRequest<OperationResult<Question52ResultDto>>
    {
        public Question52RequestDto Payload { get; set; } = new();
    }
}