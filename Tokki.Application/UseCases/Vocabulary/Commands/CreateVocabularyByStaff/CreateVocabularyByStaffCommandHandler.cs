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

namespace Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabularyByStaff
{
    public class CreateVocabularyByStaffCommandHandler
        : IRequestHandler<CreateVocabularyByStaffCommand, OperationResult<VocabularyResponse>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IVocabularyExampleRepository _vocabularyExampleRepository;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ITextToSpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CreateVocabularyByStaffCommandHandler> _logger;

        public CreateVocabularyByStaffCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IVocabularyExampleRepository vocabularyExampleRepository,
            IIdGeneratorService idGenerator,
            ITextToSpeechService ttsService,
            ICloudinaryService cloudinaryService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CreateVocabularyByStaffCommandHandler> logger)
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
            CreateVocabularyByStaffCommand request,
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

            var text = request.Text.Trim();
            var definition = request.Definition.Trim();
            var pronunciation = request.Pronunciation?.Trim();
            var imgUrl = request.ImgURL?.Trim();

            await using var transaction =
                await _vocabularyExampleRepository.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingVocab =
                    await _vocabularyRepository.GetByTextAndDefinitionAsync(text, definition);

                if (existingVocab != null)
                {
                    return OperationResult<VocabularyResponse>.Failure(
                        new List<Error>
                        {
                            new Error(
                                "Vocabulary.Duplicated",
                                $"Từ vựng '{text}' với nghĩa '{definition}' đã tồn tại."
                            )
                        },
                        400,
                        $"Từ vựng '{text}' với nghĩa '{definition}' đã tồn tại trong hệ thống."
                    );
                }

                string? audioUrl = null;
                try
                {
                    var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(text);
                    audioUrl = await _cloudinaryService.UploadAudioAsync(
                        audioBytes,
                        $"VOCAB_{Guid.NewGuid()}",
                        "tokki/vocab-audio"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Không thể tạo audio cho vocabulary (Staff): {Text}", text);
                }

                var vocabulary = new Domain.Entities.Vocabulary
                {
                    VocabularyId = _idGenerator.Generate(15),
                    Text = text,
                    Pronunciation = pronunciation,
                    Definition = definition,
                    ImgURL = imgUrl,
                    AudioURL = audioUrl,
                    CreateBy = currentUserId,
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    Status = VocabularyStatus.PendingApproval
                };

                await _vocabularyRepository.AddAsync(vocabulary);

                var exampleResponses = new List<VocabularyExampleResponse>();
                var seenSentences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (request.Examples != null && request.Examples.Any())
                {
                    foreach (var exampleDto in request.Examples)
                    {
                        var sentence = exampleDto.Sentence?.Trim();
                        if (string.IsNullOrWhiteSpace(sentence)) continue;

                        if (!seenSentences.Add(sentence)) continue;

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

                await _vocabularyRepository.SaveChangesAsync(cancellationToken);
                await _vocabularyExampleRepository.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return OperationResult<VocabularyResponse>.Success(
                    new VocabularyResponse
                    {
                        VocabularyId = vocabulary.VocabularyId,
                        Text = vocabulary.Text,
                        Pronunciation = vocabulary.Pronunciation,
                        Definition = vocabulary.Definition,
                        ImgURL = vocabulary.ImgURL,
                        AudioURL = audioUrl,
                        CreateDate = vocabulary.CreateDate,
                        Examples = exampleResponses
                    },
                    201,
                    "Tạo vocabulary thành công. Đang chờ phê duyệt."
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(ex,
                    "Lỗi khi Staff tạo vocabulary: {Text}", text);

                return OperationResult<VocabularyResponse>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    $"Lỗi hệ thống: {ex.Message}"
                );
            }
        }
    }
}
