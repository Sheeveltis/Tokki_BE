using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.MiniGame.Commands.AssignWordleVocab
{
    public class AssignWordleVocabCommandHandler : IRequestHandler<AssignWordleVocabCommand, OperationResult<bool>>
    {
        private readonly IWordleRepository _wordleRepo;

        public AssignWordleVocabCommandHandler(IWordleRepository wordleRepo)
        {
            _wordleRepo = wordleRepo;
        }

        public async Task<OperationResult<bool>> Handle(AssignWordleVocabCommand request, CancellationToken cancellationToken)
        {
            var dailyWordle = await _wordleRepo.GetDailyWordleByIdAsync(request.DailyWordleId, cancellationToken);
            if (dailyWordle == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy Wordle ngày được chỉ định.", 404);
            }

            // Kiểm tra xem đã có người dùng nào tham gia chơi chưa
            bool hasProgress = await _wordleRepo.AnyUserProgressAsync(request.DailyWordleId, cancellationToken);
            if (hasProgress)
            {
                return OperationResult<bool>.Failure("Không thể thay đổi từ vựng vì đã có người dùng tham gia giải Wordle này.", 400);
            }

            var targetVocab = await _wordleRepo.GetVocabularyByIdAsync(request.VocabularyId, cancellationToken);
            if (targetVocab == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy từ vựng được chỉ định.", 404);
            }
            
            dailyWordle.Word = targetVocab.Text;
            dailyWordle.VocabularyId = targetVocab.VocabularyId;

            await _wordleRepo.UpdateDailyWordleAsync(dailyWordle, cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}
