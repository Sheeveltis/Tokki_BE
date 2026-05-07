using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage;

namespace Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabulary
{
    public class CreateVocabularyCommandHandler
        : IRequestHandler<CreateVocabularyCommand, OperationResult<VocabularyResponse>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IVocabularyExampleRepository _vocabularyExampleRepository;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ISpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CreateVocabularyCommandHandler> _logger;

        public CreateVocabularyCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IVocabularyExampleRepository vocabularyExampleRepository,
            IIdGeneratorService idGenerator,
            ISpeechService ttsService,
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

            // Chuẩn hóa input
            var text = request.Text.Trim();
            var definition = request.Definition.Trim();
            var pronunciation = request.Pronunciation?.Trim();
            var imgUrl = request.ImgURL?.Trim();

            // 2) Tạo audio URL (có thể fail, vẫn cho tạo vocab)
            // Di chuyển ra ngoài transaction vì đây là call external service, không nên chiếm dụng transaction lâu
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

            var strategy = _vocabularyRepository.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _vocabularyExampleRepository.BeginTransactionAsync(cancellationToken);
                try
                {
                    // 1) Check trùng vocabulary (normalize Text + Definition)
                    var existingVocabs = await _vocabularyRepository.GetAllByTextAsync(text);
                    var normalizedDefinition = Normalize(definition);

                    var isDuplicate = existingVocabs?.Any(v =>
                        string.Equals(Normalize(v.Text), Normalize(text), StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(Normalize(v.Definition), normalizedDefinition, StringComparison.OrdinalIgnoreCase)
                    ) ?? false;

                    if (isDuplicate)
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

                    // 3) Tạo vocabulary
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

                    // 4) Tạo examples (nếu có)
                    var exampleResponses = new List<VocabularyExampleResponse>();
                    var skippedExamples = new List<string>();
                    var seenSentences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    if (request.Examples != null && request.Examples.Any())
                    {
                        foreach (var exampleDto in request.Examples)
                        {
                            var sentence = exampleDto.Sentence?.Trim();

                            // ✅ Câu ví dụ có thể để trống, skip nếu trống
                            if (string.IsNullOrWhiteSpace(sentence))
                                continue;

                            // Trùng trong input => skip
                            if (!seenSentences.Add(sentence))
                            {
                                skippedExamples.Add(sentence);
                                continue;
                            }

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
                        AudioURL = vocabulary.AudioURL, // Dùng audioUrl đã tạo ở trên
                        CreateDate = vocabulary.CreateDate,
                        Examples = exampleResponses
                    };

                    var message = "Tạo vocabulary thành công.";
                    if (skippedExamples.Any())
                    {
                        message += $" Đã bỏ qua {skippedExamples.Count} câu ví dụ trùng lặp.";
                    }

                    return OperationResult<VocabularyResponse>.Success(result, 201, message);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Lỗi khi tạo vocabulary: {Text}", text);

                    // Nếu là lỗi transient, rethrow để strategy retry
                    // Nếu là lỗi logic (như DB constraint), strategy có thể vẫn retry tùy config,
                    // nhưng ta muốn catch ở ngoài cùng để trả về kết quả 500 nếu retry mãi vẫn fail.
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi nghiêm trọng khi thực hiện CreateVocabularyCommandHandler: {Text}", text);
            return OperationResult<VocabularyResponse>.Failure(
                new List<Error> { AppErrors.ServerError },
                500,
                $"Lỗi hệ thống: {ex.Message}"
            );
        }
    }

        // ✅ Normalize: trim, collapse khoảng trắng giữa, lowercase
        private static string Normalize(string input)
        {
            var collapsed = Regex.Replace(input.Trim(), @"\s+", " ");
            return collapsed.ToLowerInvariant();
        }
    }
}