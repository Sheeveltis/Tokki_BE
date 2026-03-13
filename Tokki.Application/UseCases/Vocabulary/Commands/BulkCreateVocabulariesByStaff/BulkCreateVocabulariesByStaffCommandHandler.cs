using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Commands.BulkCreateVocabulariesByStaff
{
    public class BulkCreateVocabulariesByStaffCommandHandler
        : IRequestHandler<BulkCreateVocabulariesByStaffCommand, OperationResult<BulkCreateVocabulariesResponse>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IVocabularyExampleRepository _vocabularyExampleRepository;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ISpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<BulkCreateVocabulariesByStaffCommandHandler> _logger;

        public BulkCreateVocabulariesByStaffCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IVocabularyExampleRepository vocabularyExampleRepository,
            IIdGeneratorService idGenerator,
            ISpeechService ttsService,
            ICloudinaryService cloudinaryService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<BulkCreateVocabulariesByStaffCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
            _vocabularyExampleRepository = vocabularyExampleRepository;
            _idGenerator = idGenerator;
            _ttsService = ttsService;
            _cloudinaryService = cloudinaryService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<BulkCreateVocabulariesResponse>> Handle(
            BulkCreateVocabulariesByStaffCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return OperationResult<BulkCreateVocabulariesResponse>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            var response = new BulkCreateVocabulariesResponse
            {
                TotalVocabularies = request.Vocabularies.Count
            };

            var duplicateErrors = new List<string>();

            foreach (var vocabDto in request.Vocabularies)
            {
                var existingVocab = await _vocabularyRepository
                    .GetByTextAndDefinitionAsync(vocabDto.Text, vocabDto.Definition);

                if (existingVocab != null)
                {
                    duplicateErrors.Add(
                        $"Từ '{vocabDto.Text}' với nghĩa '{vocabDto.Definition}'"
                    );
                }
            }

            if (duplicateErrors.Any())
            {
                var errorMessage =
                    $"Phát hiện {duplicateErrors.Count} từ vựng bị trùng:\n" +
                    string.Join("\n", duplicateErrors.Select((e, i) => $"{i + 1}. {e}"));

                return OperationResult<BulkCreateVocabulariesResponse>.Failure(
                    new List<Error>
                    {
                        new Error("VOCABULARY_DUPLICATE", errorMessage)
                    },
                    400,
                    errorMessage
                );
            }

            await using var transaction =
                await _vocabularyExampleRepository.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var vocabDto in request.Vocabularies)
                {
                    string? audioUrl = null;
                    try
                    {
                        var audioBytes =
                            await _ttsService.SynthesizeKoreanAudioAsync(vocabDto.Text);

                        audioUrl = await _cloudinaryService.UploadAudioAsync(
                            audioBytes,
                            $"VOCAB_{Guid.NewGuid()}",
                            "tokki/vocab-audio"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Không thể tạo audio cho vocabulary (Staff): {Text}",
                            vocabDto.Text);
                    }

                    var vocabulary = new Domain.Entities.Vocabulary
                    {
                        VocabularyId = _idGenerator.Generate(15),
                        Text = vocabDto.Text,
                        Pronunciation = vocabDto.Pronunciation,
                        Definition = vocabDto.Definition,
                        ImgURL = vocabDto.ImgURL,
                        AudioURL = audioUrl,
                        CreateBy = currentUserId,
                        CreateDate = DateTime.UtcNow.AddHours(7),
                        Status = VocabularyStatus.Draft
                    };

                    await _vocabularyRepository.AddAsync(vocabulary);

                    var seenSentences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    int skippedExamples = 0;

                    if (vocabDto.Examples != null)
                    {
                        foreach (var exampleDto in vocabDto.Examples)
                        {
                            if (!seenSentences.Add(exampleDto.Sentence))
                            {
                                skippedExamples++;
                                continue;
                            }

                            var example = new Domain.Entities.VocabularyExample
                            {
                                ExampleId = _idGenerator.Generate(15),
                                VocabularyId = vocabulary.VocabularyId,
                                Sentence = exampleDto.Sentence,
                                Translation = exampleDto.Translation,
                                CreateBy = currentUserId,
                                CreateAt = DateTime.UtcNow.AddHours(7),
                                Status = VocabularyExampleStatus.Active
                            };

                            await _vocabularyExampleRepository.AddAsync(example);
                        }
                    }

                    response.Results.Add(new VocabularyCreationResult
                    {
                        Text = vocabDto.Text,
                        Definition = vocabDto.Definition,
                        VocabularyId = vocabulary.VocabularyId,
                        AudioURL = audioUrl,
                        IsSuccess = true,
                        Message = skippedExamples > 0
                            ? $"Tạo thành công. Bỏ qua {skippedExamples} câu ví dụ trùng."
                            : "Tạo thành công. Đang chờ phê duyệt."
                    });

                    response.SuccessCount++;
                }

                await _vocabularyRepository.SaveChangesAsync(cancellationToken);
                await _vocabularyExampleRepository.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return OperationResult<BulkCreateVocabulariesResponse>.Success(
                    response,
                    201,
                    $"Tạo thành công {response.SuccessCount} vocabulary. Đang chờ phê duyệt."
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(ex,
                    "Lỗi khi Staff bulk create vocabulary.");

                return OperationResult<BulkCreateVocabulariesResponse>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    $"Lỗi hệ thống: {ex.Message}"
                );
            }
        }
    }
}
