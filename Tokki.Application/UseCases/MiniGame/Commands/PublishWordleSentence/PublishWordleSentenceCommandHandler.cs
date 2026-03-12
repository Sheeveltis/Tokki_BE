using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.MiniGame.Commands.PublishWordleSentence
{
    public class PublishWordleSentenceCommandHandler : IRequestHandler<PublishWordleSentenceCommand, OperationResult<bool>>
    {
        private readonly IMiniGameRepository _repository;

        public PublishWordleSentenceCommandHandler(IMiniGameRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(PublishWordleSentenceCommand request, CancellationToken token)
        {
            var submission = await _repository.GetWordleSubmissionByIdAsync(request.SubmissionId, token);

            if (submission == null)
                return OperationResult<bool>.Failure("Không tìm thấy dữ liệu bài nộp này.", 404);

            if (submission.UserId != request.UserId)
                return OperationResult<bool>.Failure("Bạn không có quyền chỉnh sửa trạng thái của bài nộp này.", 403);

            submission.IsPublic = request.IsPublic;
            submission.IsAnonymous = request.IsAnonymous;

            await _repository.SaveChangesAsync(token);

            return OperationResult<bool>.Success(true);
        }
    }
}