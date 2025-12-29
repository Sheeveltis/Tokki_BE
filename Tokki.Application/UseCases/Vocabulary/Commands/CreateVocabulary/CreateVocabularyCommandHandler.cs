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
    public class CreateVocabularyCommandHandler : IRequestHandler<CreateVocabularyCommand, OperationResult<VocabularyResponse>>
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
            // 1. Lấy thông tin user
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return OperationResult<VocabularyResponse>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            try
            {
                // 2. Kiểm tra trùng lặp: Text + Definition
                var existingVocab = await _vocabularyRepository.GetByTextAndDefinitionAsync(
                    request.Text,
                    request.Definition
                );

                if (existingVocab != null)
                {
                    return OperationResult<VocabularyResponse>.Failure(
                        new List<Error> { new Error("VOCABULARY_DUPLICATE",
                            $"Từ vựng '{request.Text}' với nghĩa '{request.Definition}' đã tồn tại.") },
                        400,
                        $"Từ vựng '{request.Text}' với nghĩa '{request.Definition}' đã tồn tại trong hệ thống."
                    );
                }

                // 3. Tạo audio URL
                string? audioUrl = null;
                try
                {
                    var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(request.Text);
                    string folderName = "tokki/vocab-audio";
                    string fileName = $"VOCAB_{Guid.NewGuid()}";
                    audioUrl = await _cloudinaryService.UploadAudioAsync(audioBytes, fileName, folderName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể tạo audio cho vocabulary: {Text}", request.Text);
                }

                // 4. Tạo vocabulary mới
                var vocabulary = new Tokki.Domain.Entities.Vocabulary
                {
                    VocabularyId = _idGenerator.Generate(15),
                    Text = request.Text,
                    Pronunciation = request.Pronunciation,
                    Definition = request.Definition,
                    ImgURL = request.ImgURL,
                    AudioURL = audioUrl,
                    CreateBy = currentUserId,
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    Status = VocabularyStatus.Active
                };

                await _vocabularyRepository.AddAsync(vocabulary);
                await _vocabularyRepository.SaveChangesAsync(cancellationToken);

                // 5. Tạo các câu ví dụ nếu có (check trùng lặp câu mẫu)
                var exampleResponses = new List<VocabularyExampleResponse>();
                var skippedExamples = new List<string>();

                if (request.Examples != null && request.Examples.Any())
                {
                    foreach (var exampleDto in request.Examples)
                    {
                        // Check trùng lặp câu mẫu (chỉ check những câu có Status = Active)
                        var existingExample = await _vocabularyExampleRepository.GetBySentenceAsync(
                            vocabulary.VocabularyId,
                            exampleDto.Sentence
                        );

                        if (existingExample != null)
                        {
                            skippedExamples.Add(exampleDto.Sentence);
                            _logger.LogInformation(
                                "Bỏ qua câu ví dụ trùng lặp: {Sentence} cho vocabulary: {VocabId}",
                                exampleDto.Sentence,
                                vocabulary.VocabularyId
                            );
                            continue;
                        }

                        // Tạo example mới
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

                        exampleResponses.Add(new VocabularyExampleResponse
                        {
                            ExampleId = example.ExampleId,
                            Sentence = example.Sentence,
                            Translation = example.Translation
                        });
                    }

                    await _vocabularyExampleRepository.SaveChangesAsync(cancellationToken);
                }

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
                    message += $" Đã bỏ qua {skippedExamples.Count} câu ví dụ trùng lặp.";
                }

                return OperationResult<VocabularyResponse>.Success(
                    result,
                    201,
                    message
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo vocabulary: {Text}", request.Text);
                return OperationResult<VocabularyResponse>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    $"Lỗi hệ thống: {ex.Message}"
                );
            }
        }
    }
}