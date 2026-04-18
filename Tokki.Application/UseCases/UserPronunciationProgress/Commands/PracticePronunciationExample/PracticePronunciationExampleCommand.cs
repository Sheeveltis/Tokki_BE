using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.UserPronunciation.Commands.PracticePronunciationExample
{
    public class PracticePronunciationExampleCommand : IRequest<OperationResult<bool>>
    {
        public string UserId { get; set; } = null!;
        public string PronunciationExampleId { get; set; } = null!;
    }
}
