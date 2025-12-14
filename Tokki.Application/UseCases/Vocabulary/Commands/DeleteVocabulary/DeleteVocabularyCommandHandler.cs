using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Commands.DeleteVocabulary
{
    public class DeleteVocabularyCommandHandler : IRequestHandler<DeleteVocabularyCommand, OperationResult<bool>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DeleteVocabularyCommandHandler> _logger;

        public DeleteVocabularyCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IVocabularyTopicRepository vocabularyTopicRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DeleteVocabularyCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            DeleteVocabularyCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.UserUnauthorized },
                        401,
                        AppErrors.UserUnauthorized.Description
                    );
                }

                // 1. Kiểm tra vocabulary tồn tại
                var vocabulary = await _vocabularyRepository.GetByIdAsync(request.VocabularyId);

                if (vocabulary == null)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.VocabularyNotFound },
                        404,
                        AppErrors.VocabularyNotFound.Description
                    );
                }

                // 2. Kiểm tra đã bị xóa chưa
                if (vocabulary.Status == VocabularyStatus.Deleted)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.VocabularyAlreadyDeleted },
                        400,
                        AppErrors.VocabularyAlreadyDeleted.Description
                    );
                }

                // 3. Soft delete: đánh dấu status là Deleted
                vocabulary.Status = VocabularyStatus.Deleted;
                vocabulary.UpdateBy = currentUserId;
                vocabulary.UpdateDate = DateTime.UtcNow;

                // 4. Đánh dấu xóa các relationships
                var vocabTopics = await _vocabularyTopicRepository.GetByVocabularyIdAsync(vocabulary.VocabularyId);
                foreach (var vt in vocabTopics)
                {
                    vt.Status = VocabularyTopicStatus.Deleted;
                    vt.UpdateBy = currentUserId;
                    vt.UpdateDate = DateTime.UtcNow;
                    await _vocabularyTopicRepository.UpdateAsync(vt);
                }

                await _vocabularyRepository.UpdateAsync(vocabulary);
                await _vocabularyRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Đã xóa vocabulary: {VocabularyId}", request.VocabularyId);

                return OperationResult<bool>.Success(
                    true,
                    200,
                    $"Xóa vocabulary '{vocabulary.Text}' thành công."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa vocabulary: {VocabularyId}", request.VocabularyId);
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
