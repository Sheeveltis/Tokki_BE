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

namespace Tokki.Application.UseCases.Vocabulary.Commands.BulkCreateVocabularies
{
    public class BulkCreateVocabulariesCommandHandler : IRequestHandler<BulkCreateVocabulariesCommand, OperationResult<BulkCreateVocabulariesResponse>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IVocabularyExampleRepository _vocabularyExampleRepository;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ITextToSpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<BulkCreateVocabulariesCommandHandler> _logger;

        public BulkCreateVocabulariesCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IVocabularyExampleRepository vocabularyExampleRepository,
            IIdGeneratorService idGenerator,
            ITextToSpeechService ttsService,
            ICloudinaryService cloudinaryService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<BulkCreateVocabulariesCommandHandler> logger)
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
            BulkCreateVocabulariesCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Lấy thông tin user
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return OperationResult<BulkCreateVocabulariesResponse>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            var response = new BulkCreateVocabulariesResponse
            {
                TotalVocabularies = request.Vocabularies.Count
            };

            // 2. Validate trùng lặp TRƯỚC KHI bắt đầu transaction
            var duplicateErrors = new List<string>();

            foreach (var vocabDto in request.Vocabularies)
            {
                var existingVocab = await _vocabularyRepository.GetByTextAndDefinitionAsync(
                    vocabDto.Text,
                    vocabDto.Definition
                );

                if (existingVocab != null)
                {
                    duplicateErrors.Add($"Từ '{vocabDto.Text}' với nghĩa '{vocabDto.Definition}'");
                }
            }

            // Nếu có bất kỳ trùng lặp nào -> Reject toàn bộ
            if (duplicateErrors.Any())
            {
                var errorMessage = $"Phát hiện {duplicateErrors.Count} từ vựng bị trùng lặp (Text + Definition):\n" +
                                   string.Join("\n", duplicateErrors.Select((e, i) => $"{i + 1}. {e}"));

                return OperationResult<BulkCreateVocabulariesResponse>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { new Error("VOCABULARY_DUPLICATE", errorMessage) },
                    400,
                    $"Không thể tạo vocabulary. {errorMessage}"
                );
            }

            // 3. Bắt đầu Transaction - All or Nothing
            await using var transaction = await _vocabularyExampleRepository.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var vocabDto in request.Vocabularies)
                {
                    // 4. Tạo audio URL
                    string? audioUrl = null;
                    try
                    {
                        var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(vocabDto.Text);
                        string folderName = "tokki/vocab-audio";
                        string fileName = $"VOCAB_{Guid.NewGuid()}";
                        audioUrl = await _cloudinaryService.UploadAudioAsync(audioBytes, fileName, folderName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể tạo audio cho vocabulary: {Text}", vocabDto.Text);
                    }

                    // 5. Tạo vocabulary mới
                    var vocabulary = new Tokki.Domain.Entities.Vocabulary
                    {
                        VocabularyId = _idGenerator.Generate(15),
                        Text = vocabDto.Text,
                        Pronunciation = vocabDto.Pronunciation,
                        Definition = vocabDto.Definition,
                        ImgURL = vocabDto.ImgURL,
                        AudioURL = audioUrl,
                        CreateBy = currentUserId,
                        CreateDate = DateTime.UtcNow.AddHours(7),
                        Status = VocabularyStatus.Active
                    };

                    await _vocabularyRepository.AddAsync(vocabulary);

                    // 6. Tạo các câu ví dụ nếu có (check trùng lặp câu mẫu trong cùng vocabulary)
                    int addedExamplesCount = 0;
                    int skippedExamplesCount = 0;
                    var seenSentences = new HashSet<string>();

                    if (vocabDto.Examples != null && vocabDto.Examples.Any())
                    {
                        foreach (var exampleDto in vocabDto.Examples)
                        {
                            // Check trùng lặp trong cùng request
                            if (seenSentences.Contains(exampleDto.Sentence))
                            {
                                skippedExamplesCount++;
                                _logger.LogInformation(
                                    "Bỏ qua câu ví dụ trùng lặp trong request: {Sentence}",
                                    exampleDto.Sentence
                                );
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
                            seenSentences.Add(exampleDto.Sentence);
                            addedExamplesCount++;
                        }
                    }

                    var resultMessage = "Tạo vocabulary thành công.";
                    if (skippedExamplesCount > 0)
                    {
                        resultMessage += $" Đã bỏ qua {skippedExamplesCount} câu ví dụ trùng lặp.";
                    }

                    response.Results.Add(new VocabularyCreationResult
                    {
                        Text = vocabDto.Text,
                        Definition = vocabDto.Definition,
                        IsSuccess = true,
                        VocabularyId = vocabulary.VocabularyId,
                        AudioURL = audioUrl,
                        Message = resultMessage
                    });
                    response.SuccessCount++;
                }

                // 7. Lưu tất cả thay đổi
                await _vocabularyRepository.SaveChangesAsync(cancellationToken);
                await _vocabularyExampleRepository.SaveChangesAsync(cancellationToken);

                // 8. Commit transaction
                await transaction.CommitAsync(cancellationToken);

                var message = $"Tạo thành công tất cả {response.SuccessCount} từ vựng.";

                return OperationResult<BulkCreateVocabulariesResponse>.Success(
                    response,
                    201,
                    message
                );
            }
            catch (Exception ex)
            {
                // 9. Rollback nếu có lỗi
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(ex, "Lỗi khi bulk create vocabularies. Transaction đã được rollback.");

                return OperationResult<BulkCreateVocabulariesResponse>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ServerError },
                    500,
                    $"Lỗi hệ thống: {ex.Message}. Không có vocabulary nào được tạo."
                );
            }
        }
    }
}