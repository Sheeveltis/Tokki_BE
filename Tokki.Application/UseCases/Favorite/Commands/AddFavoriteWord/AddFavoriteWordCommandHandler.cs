using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Favorite.Commands.AddFavoriteWord
{
    public class AddFavoriteWordCommandHandler : IRequestHandler<AddFavoriteWordCommand, OperationResult<string>>
    {
        private readonly IUserFavoriteWordRepository _favoriteWordRepository;
        private readonly IWordRepository _wordRepository;
        private readonly IMeaningRepository _meaningRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AddFavoriteWordCommandHandler> _logger;

        public AddFavoriteWordCommandHandler(
            IUserFavoriteWordRepository favoriteWordRepository,
            IWordRepository wordRepository,
            IMeaningRepository meaningRepository,
            IIdGeneratorService idGeneratorService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AddFavoriteWordCommandHandler> logger)
        {
            _favoriteWordRepository = favoriteWordRepository;
            _wordRepository = wordRepository;
            _meaningRepository = meaningRepository;
            _idGeneratorService = idGeneratorService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(
            AddFavoriteWordCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("Không xác định được người dùng.");

            try
            {
                // Kiểm tra Word tồn tại
                var word = await _wordRepository.GetByIdAsync(request.WordId);
                if (word == null)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.WordNotFound },
                        404,
                        AppErrors.WordNotFound.Description
                    );
                }

                // Kiểm tra Meaning nếu có
                if (!string.IsNullOrEmpty(request.MeaningId))
                {
                    var meaning = await _meaningRepository.GetByIdAsync(request.MeaningId);
                    if (meaning == null || meaning.WordId != request.WordId)
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.MeaningNotFound },
                            404,
                            AppErrors.MeaningNotFound.Description
                        );
                    }
                }

                // Kiểm tra đã tồn tại chưa
                var existing = await _favoriteWordRepository.GetByUserAndWordAsync(currentUserId, request.WordId);
                if (existing != null)
                {
                    if (existing.Status == UserFavoriteWordStatus.Active)
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.FavoriteWordAlreadyExists },
                            400,
                            AppErrors.FavoriteWordAlreadyExists.Description
                        );
                    }

                    // Nếu đã bỏ favorite trước đó, kích hoạt lại
                    existing.Status = UserFavoriteWordStatus.Active;
                    existing.MeaningId = request.MeaningId;
                    existing.Note = request.Note;
                    existing.CreateDate = DateTime.UtcNow.AddHours(7);

                    await _favoriteWordRepository.UpdateAsync(existing);
                    await _favoriteWordRepository.SaveChangesAsync(cancellationToken);

                    return OperationResult<string>.Success(
                        existing.FavoriteWordId,
                        200,
                        "Đã thêm lại từ vựng vào danh sách yêu thích."
                    );
                }

                // Tạo mới với ID generator
                string newId = _idGeneratorService.GenerateCustom(15);

                var favoriteWord = new UserFavoriteWord
                {
                    FavoriteWordId = newId,
                    UserId = currentUserId,
                    WordId = request.WordId,
                    MeaningId = request.MeaningId,
                    Note = request.Note,
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    Status = UserFavoriteWordStatus.Active
                };

                await _favoriteWordRepository.AddAsync(favoriteWord);
                await _favoriteWordRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    favoriteWord.FavoriteWordId,
                    201,
                    "Thêm từ vựng vào danh sách yêu thích thành công."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm từ vựng yêu thích: {WordId}", request.WordId);
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}