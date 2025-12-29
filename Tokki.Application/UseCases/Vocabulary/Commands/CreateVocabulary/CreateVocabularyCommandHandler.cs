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

namespace Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabulary
{
    public class CreateVocabularyCommandHandler
        : IRequestHandler<CreateVocabularyCommand, OperationResult<VocabularyResponse>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IVocabularyExampleRepository _vocabularyExampleRepository;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ITextToSpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CreateVocabularyCommandHandler> _logger;

        public CreateVocabularyCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IVocabularyExampleRepository vocabularyExampleRepository,
            IIdGeneratorService idGenerator,
            ITextToSpeechService ttsService,
            ICloudinaryService cloudinaryService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CreateVocabularyCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
            _vocabularyExampleRepository = vocabularyExampleRepository;
            _idGenerator = idGenerator;
            _ttsService = ttsService;
            _cloudinaryService = cloudinaryService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<VocabularyResponse>> Handle(
            CreateVocabularyCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return OperationResult<VocabularyResponse>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            // Chuẩn hóa input (validator đã check not empty/length)
            var text = request.Text.Trim();
            var definition = request.Definition.Trim();
            var pronunciation = request.Pronunciation?.Trim();
            var imgUrl = request.ImgURL?.Trim();

            // Transaction: vocab + examples all-or-nothing
            await using var transaction = await _vocabularyExampleRepository.BeginTransactionAsync(cancellationToken);

            try
            {
                // 1) Check trùng vocabulary (Text + Definition)
                var existingVocab = await _vocabularyRepository.GetByTextAndDefinitionAsync(text, definition);
                if (existingVocab != null)
                {
                    return OperationResult<VocabularyResponse>.Failure(
                        new List<Error>
                        {
                            new Error("Vocabulary.Duplicated",
                                $"Từ vựng '{text}' với nghĩa '{definition}' đã tồn tại.")
                        },
                        400,
                        $"Từ vựng '{text}' với nghĩa '{definition}' đã tồn tại trong hệ thống."
                    );
                }

                // 2) Tạo audio URL (có thể fail, vẫn cho tạo vocab)
                string? audioUrl = null;
                try
                {
                    var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(text);
                    var folderName = "tokki/vocab-audio";
                    var fileName = $"VOCAB_{Guid.NewGuid()}";
                    audioUrl = await _cloudinaryService.UploadAudioAsync(audioBytes, fileName, folderName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể tạo audio cho vocabulary: {Text}", text);
                }

                // 3) Tạo vocabulary (mặc định Active theo nghiệp vụ)
                var vocabulary = new Tokki.Domain.Entities.Vocabulary
                {
                    VocabularyId = _idGenerator.Generate(15),
                    Text = text,
                    Pronunciation = pronunciation,
                    Definition = definition,
                    ImgURL = imgUrl,
                    AudioURL = audioUrl,
                    CreateBy = currentUserId,
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    Status = VocabularyStatus.Active
                };

                await _vocabularyRepository.AddAsync(vocabulary);

                // 4) Tạo examples (nếu có) + check trùng ngay trong input
                var exampleResponses = new List<VocabularyExampleResponse>();
                var skippedExamples = new List<string>();

                // Normalize + check duplicate in input (Trim + ignore-case)
                var seenSentences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (request.Examples != null && request.Examples.Any())
                {
                    foreach (var exampleDto in request.Examples)
                    {
                        var sentence = exampleDto.Sentence?.Trim();

                        // Validator thường đã bắt, nhưng giữ an toàn
                        if (string.IsNullOrWhiteSpace(sentence))
                            continue;

                        // Trùng trong input => skip và ghi nhận
                        if (!seenSentences.Add(sentence))
                        {
                            skippedExamples.Add(sentence);
                            continue;
                        }

                        // Với vocab mới thì DB gần như không thể có examples cùng VocabularyId,
                        // nhưng vẫn giữ check để tránh trường hợp ID collision hiếm.
                        var existingExample = await _vocabularyExampleRepository.GetBySentenceAsync(
                            vocabulary.VocabularyId,
                            sentence
                        );

                        if (existingExample != null)
                        {
                            skippedExamples.Add(sentence);
                            continue;
                        }

                        var example = new Domain.Entities.VocabularyExample
                        {
                            ExampleId = _idGenerator.Generate(15),
                            VocabularyId = vocabulary.VocabularyId,
                            Sentence = sentence,
                            Translation = exampleDto.Translation?.Trim(),
                            CreateBy = currentUserId,
                            CreateAt = DateTime.UtcNow.AddHours(7),
                            Status = VocabularyExampleStatus.Active
                        };

                        await _vocabularyExampleRepository.AddAsync(example);

                        exampleResponses.Add(new VocabularyExampleResponse
                        {
                            ExampleId = example.ExampleId,
                            Sentence = example.Sentence,
                            Translation = example.Translation
                        });
                    }
                }

                // 5) Save all + commit
                await _vocabularyRepository.SaveChangesAsync(cancellationToken);
                await _vocabularyExampleRepository.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var result = new VocabularyResponse
                {
                    VocabularyId = vocabulary.VocabularyId,
                    Text = vocabulary.Text,
                    Pronunciation = vocabulary.Pronunciation,
                    Definition = vocabulary.Definition,
                    ImgURL = vocabulary.ImgURL,
                    AudioURL = audioUrl,
                    CreateDate = vocabulary.CreateDate,
                    Examples = exampleResponses
                };

                var message = "Tạo vocabulary thành công.";
                if (skippedExamples.Any())
                {
                    // Nếu muốn báo cụ thể câu nào trùng:
                    // message += $" Đã bỏ qua {skippedExamples.Count} câu ví dụ trùng: {string.Join(" | ", skippedExamples)}.";
                    message += $" Đã bỏ qua {skippedExamples.Count} câu ví dụ trùng lặp.";
                }

                return OperationResult<VocabularyResponse>.Success(result, 201, message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(ex, "Lỗi khi tạo vocabulary: {Text}", text);
                return OperationResult<VocabularyResponse>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    $"Lỗi hệ thống: {ex.Message}"
                );
            }
        }
    }
}
