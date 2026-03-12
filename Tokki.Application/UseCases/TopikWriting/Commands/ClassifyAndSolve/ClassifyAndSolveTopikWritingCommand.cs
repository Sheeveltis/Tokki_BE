using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.TopikWriting.DTOs;

namespace Tokki.Application.UseCases.TopikWriting.Commands.ClassifyAndSolve
{
    public sealed class ClassifyAndSolveTopikWritingCommand
        : IRequest<OperationResult<TopikWritingResultDto>>
    {
        public TopikWritingRequestDto Payload { get; set; } = new();
    }
}
