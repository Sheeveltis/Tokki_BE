using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic
{
    public class RemoveVocabulariesFromTopicCommandHandler
        : IRequestHandler<RemoveVocabulariesFromTopicCommand, OperationResult<int>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidator<RemoveVocabulariesFromTopicCommand> _validator;

        public RemoveVocabulariesFromTopicCommandHandler(
            ITopicRepository topicRepository,
            IVocabularyTopicRepository vocabularyTopicRepository,
            IHttpContextAccessor httpContextAccessor,
            IValidator<RemoveVocabulariesFromTopicCommand> validator)
        {
            _topicRepository = topicRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
            _httpContextAccessor = httpContextAccessor;
            _validator = validator;
        }

        public async Task<OperationResult<int>> Handle(
            RemoveVocabulariesFromTopicCommand request,
            CancellationToken cancellationToken)
        {
            // 1. VALIDATION
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => new Error(e.ErrorCode, e.ErrorMessage))
                    .ToList();

                return OperationResult<int>.Failure(
                    errors,
                    400,
                    AppErrors.ValidationFailed.Description);
            }

            // 2. CHECK TOPIC EXISTENCE
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<int>.Failure(
                    new List<Error> { AppErrors.TopicNotFound },
                    404,
                    AppErrors.TopicNotFound.Description);
            }

            // 3. GET CURRENT USER
            var currentUserId = _httpContextAccessor.HttpContext?
                .User?
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            // 4. EXECUTE TRANSACTION
            var (success, removedCount, failedItems) =
              await _vocabularyTopicRepository.SoftRemoveVocabulariesFromTopicAsync(
                  request.TopicId,
                  request.VocabularyIds,
                  currentUserId,
                  cancellationToken);

            // 5. RETURN RESULT
            if (!success)
            {
                return OperationResult<int>.Failure(
                    new List<Error>
                    {
                        new Error(
                            "REMOVE_VOCABULARIES_FAILED",
                            $"Không thể gỡ các từ vựng:\n{string.Join("\n", failedItems)}")
                    },
                    400,
                    "Giao dịch đã bị hủy. Không có từ vựng nào được gỡ khỏi chủ đề."
                );
            }

            if (removedCount == 0)
            {
                return OperationResult<int>.Success(
                    0,
                    200,
                    "Các từ vựng không tồn tại trong chủ đề. Không có thay đổi nào được thực hiện."
                );
            }

            return OperationResult<int>.Success(
                removedCount,
                200,
                $"Đã gỡ thành công {removedCount} từ vựng khỏi chủ đề '{topic.TopicName}'."
            );
        }
    }
}
