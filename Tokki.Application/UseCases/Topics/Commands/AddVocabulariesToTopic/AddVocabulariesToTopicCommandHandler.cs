using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.AddVocabulariesToTopic
{
    public class AddVocabulariesToTopicCommandHandler
        : IRequestHandler<AddVocabulariesToTopicCommand, OperationResult<int>>
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

        public async Task<OperationResult<int>> Handle(
            AddVocabulariesToTopicCommand request,
            CancellationToken cancellationToken)
        {
            // 1) VALIDATION
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => new Error(e.ErrorCode, e.ErrorMessage))
                    .ToList();

                return OperationResult<int>.Failure(errors, 400, AppErrors.ValidationFailed.Description);
            }

            // Chống trùng ID đầu vào
            var requestedIds = request.VocabularyIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            // 2) CHECK TOPIC EXISTENCE
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<int>.Failure(
                    new List<Error> { AppErrors.TopicNotFound },
                    404,
                    AppErrors.TopicNotFound.Description);
            }

            // 3) GET VOCABULARIES (chỉ lấy được những ID tồn tại)
            var foundVocabularies = await _vocabularyRepository.GetByIdsAsync(requestedIds);

            if (!foundVocabularies.Any())
            {
                return OperationResult<int>.Failure(
                    new List<Error> { AppErrors.NoValidVocabulariesFound },
                    400,
                    AppErrors.NoValidVocabulariesFound.Description);
            }

            var foundIds = foundVocabularies.Select(v => v.VocabularyId).ToHashSet();
            var notFoundIds = requestedIds.Where(id => !foundIds.Contains(id)).ToList();

            // 4) EXECUTE (NO TRANSACTION)
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var (addedOrReactivated, skippedAlreadyActive, failedItems) =
                await _vocabularyTopicRepository.AddOrReactivateVocabulariesToTopicAsync(
                    request.TopicId,
                    foundVocabularies,
                    currentUserId,
                    cancellationToken);

            // 5) BUILD RESULT MESSAGE
            var totalRequested = requestedIds.Count;
            var notAddedCount = totalRequested - addedOrReactivated;

            var lines = new List<string>
            {
                $"Kết quả thêm từ vựng vào chủ đề '{topic.TopicName}':",
                $"- Thêm/Re-activate thành công: {addedOrReactivated}",
                $"- Không thêm: {notAddedCount}"
            };

            if (skippedAlreadyActive > 0)
                lines.Add($"  + Bỏ qua do đã Active: {skippedAlreadyActive}");

            if (notFoundIds.Any())
                lines.Add($"  + Không tồn tại trong hệ thống: {notFoundIds.Count} (IDs: {string.Join(", ", notFoundIds)})");

            if (failedItems.Any())
                lines.Add($"  + Không thể thêm (ví dụ vocab đã bị xóa): {failedItems.Count}\n    - {string.Join("\n    - ", failedItems)}");

            var message = string.Join("\n", lines);

            return OperationResult<int>.Success(
                addedOrReactivated,
                200,
                message);
        }
    }
}
