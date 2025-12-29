using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.VocabularyExample.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.VocabularyExample.Commands.AddExamples
{
    public class AddVocabularyExamplesCommandHandler
        : IRequestHandler<AddVocabularyExamplesCommand, OperationResult<AddVocabularyExamplesResponse>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IVocabularyExampleRepository _exampleRepository;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AddVocabularyExamplesCommandHandler> _logger;

        public AddVocabularyExamplesCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IVocabularyExampleRepository exampleRepository,
            IIdGeneratorService idGenerator,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AddVocabularyExamplesCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
            _exampleRepository = exampleRepository;
            _idGenerator = idGenerator;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<AddVocabularyExamplesResponse>> Handle(
            AddVocabularyExamplesCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return OperationResult<AddVocabularyExamplesResponse>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            if (string.IsNullOrWhiteSpace(request.VocabularyId))
            {
                return OperationResult<AddVocabularyExamplesResponse>.Failure(
                    new List<Error> { AppErrors.VocabularyIdEmpty },
                    400,
                    AppErrors.VocabularyIdEmpty.Description
                );
            }

            if (request.Examples == null || !request.Examples.Any())
            {
                return OperationResult<AddVocabularyExamplesResponse>.Failure(
                    new List<Error> { AppErrors.ExamplesEmpty },
                    400,
                    AppErrors.ExamplesEmpty.Description
                );
            }

            // Check vocabulary tồn tại
            var vocab = await _vocabularyRepository.GetByIdAsync(request.VocabularyId);
            if (vocab == null)
            {
                return OperationResult<AddVocabularyExamplesResponse>.Failure(
                    new List<Error> { AppErrors.VocabularyNotFound },
                    404,
                    AppErrors.VocabularyNotFound.Description
                );
            }

            var created = new List<VocabularyExampleResponse>();
            var skipped = new List<string>();

            foreach (var dto in request.Examples)
            {
                var sentence = dto.Sentence?.Trim();
                if (string.IsNullOrWhiteSpace(sentence))
                {
                    // Bạn có thể chọn: fail ngay hoặc skip.
                    // Ở đây chọn fail để dữ liệu sạch.
                    return OperationResult<AddVocabularyExamplesResponse>.Failure(
                        new List<Error> { AppErrors.ExampleSentenceEmpty },
                        400,
                        AppErrors.ExampleSentenceEmpty.Description
                    );
                }

                // Check duplicate (chỉ trong Active như bạn đang làm)
                var existing = await _exampleRepository.GetBySentenceAsync(request.VocabularyId, sentence);
                if (existing != null)
                {
                    skipped.Add(sentence);
                    continue;
                }

                var example = new Domain.Entities.VocabularyExample
                {
                    ExampleId = _idGenerator.Generate(15),
                    VocabularyId = request.VocabularyId,
                    Sentence = sentence,
                    Translation = dto.Translation?.Trim(),
                    CreateBy = currentUserId,
                    CreateAt = DateTime.UtcNow.AddHours(7),
                    Status = VocabularyExampleStatus.Active
                };

                await _exampleRepository.AddAsync(example);

                created.Add(new VocabularyExampleResponse
                {
                    ExampleId = example.ExampleId,
                    Sentence = example.Sentence,
                    Translation = example.Translation
                });
            }

            await _exampleRepository.SaveChangesAsync(cancellationToken);

            var response = new AddVocabularyExamplesResponse
            {
                VocabularyId = request.VocabularyId,
                CreatedExamples = created,
                SkippedSentences = skipped
            };

            var message = $"Thêm câu ví dụ thành công: {created.Count}.";
            if (skipped.Any())
                message += $" Bỏ qua trùng lặp: {skipped.Count}.";

            _logger.LogInformation(
                "Add examples to vocab {VocabId}. Created={CreatedCount}, Skipped={SkippedCount}, User={UserId}",
                request.VocabularyId, created.Count, skipped.Count, currentUserId);

            return OperationResult<AddVocabularyExamplesResponse>.Success(response, 201, message);
        }
    }
}
