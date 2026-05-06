using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.MiniGame.Commands.ToggleWordleSentenceVisibility
{
    public class ToggleWordleSentenceVisibilityCommand : IRequest<OperationResult<bool>>
    {
        public string SubmissionId { get; set; } = string.Empty;
        public bool? IsPublic { get; set; } // If null, toggle the current state
    }
}
