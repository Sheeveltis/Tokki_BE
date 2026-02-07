using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Commands.DeleteVocabulary
{
    public class DeleteVocabularyCommandHandler : IRequestHandler<DeleteVocabularyCommand, OperationResult<bool>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;
        private readonly IVocabularyExampleRepository _vocabularyExampleRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DeleteVocabularyCommandHandler> _logger;

        public DeleteVocabularyCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IVocabularyTopicRepository vocabularyTopicRepository,
            IVocabularyExampleRepository vocabularyExampleRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DeleteVocabularyCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
            _vocabularyExampleRepository = vocabularyExampleRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            DeleteVocabularyCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = _httpContextAccessor.HttpContext?
                    .User?
                    .FindFirst(ClaimTypes.NameIdentifier)?
                    .Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.UserUnauthorized },
                        401,
                        AppErrors.UserUnauthorized.Description
                    );
                }

                // 1) Check vocabulary tồn tại
                // Khuyến nghị dùng GetByIdWithChildrenAsync (Include Topics + Examples) để update 1 lần.
                var vocabulary = await _vocabularyRepository.GetByIdWithChildrenAsync(request.VocabularyId);
                if (vocabulary == null)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.VocabularyNotFound },
                        404,
                        AppErrors.VocabularyNotFound.Description
                    );
                }

                // 2) Đã bị xóa chưa
                if (vocabulary.Status == VocabularyStatus.Deleted)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.VocabularyAlreadyDeleted },
                        400,
                        AppErrors.VocabularyAlreadyDeleted.Description
                    );
                }

                // 3) Soft delete vocabulary + cascade children => Deleted
                vocabulary.Status = VocabularyStatus.Deleted;
                vocabulary.UpdateBy = currentUserId;
                vocabulary.UpdateDate = DateTime.UtcNow.AddHours(7);

                // Cascade VocabularyTopics => Deleted
                foreach (var vt in vocabulary.VocabularyTopics)
                {
                    vt.Status = VocabularyTopicStatus.Deleted;
                    vt.UpdateBy = currentUserId;
                    vt.UpdateDate = DateTime.UtcNow.AddHours(7);
                }

                // Cascade VocabularyExamples => Deleted
                foreach (var ex in vocabulary.VocabularyExamples)
                {
                    ex.Status = VocabularyExampleStatus.Deleted;
                }

                await _vocabularyRepository.UpdateAsync(vocabulary);
                await _vocabularyRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Đã xóa vocabulary {VocabularyId} (cascade topics/examples)", request.VocabularyId);

                return OperationResult<bool>.Success(
                    true,
                    200,
                    $"Xóa vocabulary '{vocabulary.Text}' thành công."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa vocabulary {VocabularyId}", request.VocabularyId);

                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
