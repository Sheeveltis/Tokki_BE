// Application/UseCases/TopikWriting/Question51/Commands/SolveQuestion51Command.cs
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.TopikWriting.DTOs;
using Tokki.Application.UseCases.TopikWriting.Question51.DTOs;

namespace Tokki.Application.UseCases.TopikWriting.Question51.Commands
{
    public sealed class SolveQuestion51Command : IRequest<OperationResult<Question51ResultDto>>
    {
        public Question51RequestDto Payload { get; set; } = new();
    }
}