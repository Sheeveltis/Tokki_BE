using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Topics.Commands.AddVocabulariesToTopic
{
    public class AddVocabulariesToTopicCommandHandler : IRequestHandler<AddVocabulariesToTopicCommand, OperationResult<int>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidator<AddVocabulariesToTopicCommand> _validator;

        public AddVocabulariesToTopicCommandHandler(
            ITopicRepository topicRepository,
            IVocabularyRepository vocabularyRepository,
            IVocabularyTopicRepository vocabularyTopicRepository,
            IHttpContextAccessor httpContextAccessor,
            IValidator<AddVocabulariesToTopicCommand> validator)
        {
            _topicRepository = topicRepository;
            _vocabularyRepository = vocabularyRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
            _httpContextAccessor = httpContextAccessor;
            _validator = validator;
        }

        public async Task<OperationResult<int>> Handle(AddVocabulariesToTopicCommand request, CancellationToken cancellationToken)
        {
            // 1. VALIDATION
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => new Error(e.ErrorCode, e.ErrorMessage))
                    .ToList();

                return OperationResult<int>.Failure(errors, 400, AppErrors.ValidationFailed.Description);
            }

            // 2. CHECK TOPIC EXISTENCE
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<int>.Failure(
                    new List<Error> { AppErrors.TopicNotFound },
                    404,
                    AppErrors.TopicNotFound.Description
                );
            }

            // 3. GET VOCABULARIES
            var validVocabularies = await _vocabularyRepository.GetByIdsAsync(request.VocabularyIds);

            if (!validVocabularies.Any())
            {
                return OperationResult<int>.Failure(
                    new List<Error> { AppErrors.NoValidVocabulariesFound },
                    400,
                    AppErrors.NoValidVocabulariesFound.Description
                );
            }

            // Kiểm tra những từ vựng không tồn tại
            var foundVocabIds = validVocabularies.Select(v => v.VocabularyId).ToList();
            var notFoundVocabIds = request.VocabularyIds.Except(foundVocabIds).ToList();

            if (notFoundVocabIds.Any())
            {
                return OperationResult<int>.Failure(
                    new List<Error>
                    {
                        new Error("VOCABULARY_NOT_FOUND",
                            $"Các từ vựng sau không tồn tại: {string.Join(", ", notFoundVocabIds)}")
                    },
                    400,
                    "Một số từ vựng không tồn tại trong hệ thống."
                );
            }

            // 4. EXECUTE WITH TRANSACTION (gọi method từ repository)
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var (success, addedCount, failedItems) = await _vocabularyTopicRepository
                .AddVocabulariesToTopicWithTransactionAsync(
                    request.TopicId,
                    validVocabularies,
                    currentUserId,
                    cancellationToken);

            // 5. RETURN RESULT
            if (!success)
            {
                return OperationResult<int>.Failure(
                    new List<Error>
                    {
                        new Error("ADD_VOCABULARIES_FAILED",
                            $"Không thể thêm các từ vựng:\n{string.Join("\n", failedItems)}")
                    },
                    400,
                    "Giao dịch đã bị hủy. Không có từ vựng nào được thêm vào chủ đề."
                );
            }

            if (addedCount == 0)
            {
                return OperationResult<int>.Success(
                    0,
                    200,
                    "Tất cả các từ vựng đã tồn tại trong chủ đề. Không có thay đổi nào được thực hiện."
                );
            }

            return OperationResult<int>.Success(
                addedCount,
                200,
                $"Đã thêm thành công {addedCount}/{validVocabularies.Count} từ vựng vào chủ đề '{topic.TopicName}'."
            );
        }
    }
}
