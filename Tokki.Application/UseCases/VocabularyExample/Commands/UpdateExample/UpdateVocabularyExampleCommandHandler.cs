using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.VocabularyExample.Commands.UpdateExample
{
    public class UpdateVocabularyExampleCommandHandler
        : IRequestHandler<UpdateVocabularyExampleCommand, OperationResult<VocabularyExampleResponse>>
    {
        private readonly IVocabularyExampleRepository _exampleRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UpdateVocabularyExampleCommandHandler> _logger;

        public UpdateVocabularyExampleCommandHandler(
            IVocabularyExampleRepository exampleRepo,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UpdateVocabularyExampleCommandHandler> logger)
        {
            _exampleRepo = exampleRepo;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<VocabularyExampleResponse>> Handle(
            UpdateVocabularyExampleCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return OperationResult<VocabularyExampleResponse>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            var example = await _exampleRepo.GetByIdAsync(request.ExampleId);
            if (example == null)
            {
                return OperationResult<VocabularyExampleResponse>.Failure(
                    new List<Error> { AppErrors.ExampleNotFound },
                    404,
                    AppErrors.ExampleNotFound.Description
                );
            }

            // Sentence: nếu client truyền null -> bỏ qua
            // nếu client truyền "" / "   " -> bỏ qua (không đổi)
            // nếu có giá trị thật -> update + check duplicate
            if (request.UpdateData.Sentence != null)
            {
                var newSentence = request.UpdateData.Sentence.Trim();

                if (!string.IsNullOrWhiteSpace(newSentence)
                    && !string.Equals(example.Sentence, newSentence, StringComparison.Ordinal))
                {
                    var dup = await _exampleRepo.GetBySentenceAsync(example.VocabularyId, newSentence);
                    if (dup != null && dup.ExampleId != example.ExampleId)
                    {
                        return OperationResult<VocabularyExampleResponse>.Failure(
                            new List<Error> { AppErrors.ExampleDuplicate },
                            400,
                            AppErrors.ExampleDuplicate.Description
                        );
                    }

                    example.Sentence = newSentence;
                }
            }

            // Translation: tương tự
            if (request.UpdateData.Translation != null)
            {
                var newTranslation = request.UpdateData.Translation.Trim();

                // Nếu muốn "truyền rỗng thì không đổi" -> chỉ update khi có nội dung
                if (!string.IsNullOrWhiteSpace(newTranslation))
                {
                    example.Translation = newTranslation;
                }
            }

            // Status: chỉ update khi HasValue
            if (request.UpdateData.Status.HasValue)
            {
                example.Status = request.UpdateData.Status.Value;
            }

            await _exampleRepo.UpdateAsync(example);
            await _exampleRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated example {ExampleId} by user {UserId}",
                request.ExampleId, currentUserId);

            var response = new VocabularyExampleResponse
            {
                ExampleId = example.ExampleId,
                Sentence = example.Sentence,
                Translation = example.Translation
            };

            return OperationResult<VocabularyExampleResponse>.Success(
                response,
                200,
                "Cập nhật câu ví dụ thành công"
            );
        }
    }
}
