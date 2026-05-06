using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.MiniGame.Commands.ToggleWordleSentenceVisibility
{
    public class ToggleWordleSentenceVisibilityCommandHandler : IRequestHandler<ToggleWordleSentenceVisibilityCommand, OperationResult<bool>>
    {
        private readonly IMiniGameRepository _repository;

        public ToggleWordleSentenceVisibilityCommandHandler(IMiniGameRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(ToggleWordleSentenceVisibilityCommand request, CancellationToken token)
        {
            var submission = await _repository.GetWordleSubmissionByIdAsync(request.SubmissionId, token);

            if (submission == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy lượt nộp bài này.", 404);
            }

            if (request.IsPublic.HasValue)
            {
                submission.IsPublic = request.IsPublic.Value;
            }
            else
            {
                submission.IsPublic = !submission.IsPublic;
            }

            await _repository.SaveChangesAsync(token);

            return OperationResult<bool>.Success(submission.IsPublic);
        }
    }
}
