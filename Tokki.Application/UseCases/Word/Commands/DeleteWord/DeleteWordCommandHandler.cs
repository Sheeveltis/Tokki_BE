
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Word.Commands.DeleteWord
{
    public class DeleteWordCommandHandler : IRequestHandler<DeleteWordCommand, OperationResult<bool>>
    {
        private readonly IWordRepository _wordRepository;
        private readonly IMeaningRepository _meaningRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DeleteWordCommandHandler(
            IWordRepository wordRepository,
            IMeaningRepository meaningRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _wordRepository = wordRepository;
            _meaningRepository = meaningRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<bool>> Handle(
            DeleteWordCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("Không xác định được người dùng.");

            // Kiểm tra Word tồn tại
            var word = await _wordRepository.GetByIdAsync(request.WordId);
            if (word == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.WordNotFound }
                );
            }

            // Lấy tất cả meanings của word
            var meanings = await _meaningRepository.GetByWordIdAsync(request.WordId);

            // Kiểm tra xem Word có đang được sử dụng không (chỉ khi xóa mềm)
            if (meanings.Any() && !request.ForceDelete)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.WordInUse }
                );
            }

            if (request.ForceDelete)
            {
                // XÓA CỨNG: Xóa vĩnh viễn khỏi database
                foreach (var meaning in meanings)
                {
                    await _meaningRepository.DeleteByIdAsync(meaning.MeaningId);
                }

                // Xóa Word (giả sử WordRepository có method DeleteAsync)
                await _wordRepository.DeleteAsync(word);
            }
            else
            {
                // XÓA MỀM: Chỉ đổi Status thành Deleted
                word.Status = WordStatus.Deleted;
                word.UpdateBy = currentUserId;
                word.UpdateDate = DateTime.UtcNow;
                await _wordRepository.UpdateAsync(word);

                // Xóa mềm tất cả meanings
                foreach (var meaning in meanings)
                {
                    await _meaningRepository.SoftDeleteAsync(meaning.MeaningId, currentUserId);
                }
            }

            await _wordRepository.SaveChangesAsync(cancellationToken);

            string message = request.ForceDelete
                ? "Xóa vĩnh viễn từ vựng thành công."
                : "Xóa từ vựng thành công.";

            return OperationResult<bool>.Success(true, 200, message);
        }
    }
}
