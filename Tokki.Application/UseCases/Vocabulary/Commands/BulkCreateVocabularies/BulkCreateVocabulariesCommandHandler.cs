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
        private readonly ITopicRepository _topicRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ITextToSpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<BulkCreateVocabulariesCommandHandler> _logger;

        public BulkCreateVocabulariesCommandHandler(
            IVocabularyRepository vocabularyRepository,
            ITopicRepository topicRepository,
            IVocabularyTopicRepository vocabularyTopicRepository,
            IIdGeneratorService idGenerator,
            ITextToSpeechService ttsService,
            ICloudinaryService cloudinaryService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<BulkCreateVocabulariesCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
            _topicRepository = topicRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
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
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            var response = new BulkCreateVocabulariesResponse
            {
                TotalVocabularies = request.Vocabularies.Count
            };

            foreach (var vocabDto in request.Vocabularies)
            {
                try
                {
                    // 2. Validate tất cả topics tồn tại (nếu có)
                    if (vocabDto.TopicIds != null && vocabDto.TopicIds.Any())
                    {
                        var invalidTopics = new List<string>();
                        foreach (var topicId in vocabDto.TopicIds)
                        {
                            var topic = await _topicRepository.GetByIdAsync(topicId);

                            // Kiểm tra topic tồn tại VÀ chưa bị xóa
                            if (topic == null || topic.Status == TopicStatus.Deleted)
                            {
                                invalidTopics.Add(topicId);
                            }
                        }

                        if (invalidTopics.Any())
                        {
                            response.Results.Add(new VocabularyCreationResult
                            {
                                Text = vocabDto.Text,
                                Definition = vocabDto.Definition,
                                IsSuccess = false,
                                ErrorMessage = $"Topic không tồn tại hoặc đã bị xóa: {string.Join(", ", invalidTopics)}"
                            });
                            response.FailedCount++;
                            continue;
                        }
                    }

                    // 3. Kiểm tra vocabulary đã tồn tại chưa
                    var existingVocab = await _vocabularyRepository.GetByTextAndDefinitionAsync(
                        vocabDto.Text,
                        vocabDto.Definition
                    );

                    if (existingVocab != null)
                    {
                        // Vocabulary đã tồn tại
                        if (existingVocab.Status == VocabularyStatus.Deleted)
                        {
                            response.Results.Add(new VocabularyCreationResult
                            {
                                Text = vocabDto.Text,
                                Definition = vocabDto.Definition,
                                IsSuccess = false,
                                ErrorMessage = "Vocabulary đã tồn tại nhưng đang ở trạng thái đã xóa."
                            });
                            response.FailedCount++;
                            continue;
                        }

                        int addedTopicsCount = 0;
                        if (vocabDto.TopicIds != null && vocabDto.TopicIds.Any())
                        {
                            foreach (var topicId in vocabDto.TopicIds)
                            {
                                var existingVocabTopic = await _vocabularyTopicRepository
                                    .GetByVocabularyAndTopicAsync(existingVocab.VocabularyId, topicId);

                                if (existingVocabTopic == null)
                                {
                                    var vocabTopic = new VocabularyTopic
                                    {
                                        VocabularyId = existingVocab.VocabularyId,
                                        TopicId = topicId,
                                        CreateBy = currentUserId,
                                        CreateDate = DateTime.UtcNow.AddHours(7),
                                        Status = VocabularyTopicStatus.Active
                                    };
                                    await _vocabularyTopicRepository.AddAsync(vocabTopic);
                                    addedTopicsCount++;
                                }
                            }
                        }

                        var existMessage = vocabDto.TopicIds != null && vocabDto.TopicIds.Any()
                            ? $"Vocabulary đã tồn tại. Đã thêm vào {addedTopicsCount} topic mới."
                            : "Vocabulary đã tồn tại (không có topic nào được thêm).";

                        response.Results.Add(new VocabularyCreationResult
                        {
                            Text = vocabDto.Text,
                            Definition = vocabDto.Definition,
                            IsSuccess = true,
                            VocabularyId = existingVocab.VocabularyId,
                            Message = existMessage
                        });
                        response.SuccessCount++;
                        continue;
                    }

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

                    var vocabulary = new Tokki.Domain.Entities.Vocabulary
                    {
                        VocabularyId = _idGenerator.Generate(15),
                        Text = vocabDto.Text,
                        Pronunciation = vocabDto.Pronunciation,
                        AudioURL = audioUrl,
                        Definition = vocabDto.Definition,
                        ExampleSentence = vocabDto.ExampleSentence,
                        ImgURL = vocabDto.ImgURL,
                        CreateBy = currentUserId,
                        CreateDate = DateTime.UtcNow.AddHours(7),
                        Status = VocabularyStatus.Active
                    };

                    await _vocabularyRepository.AddAsync(vocabulary);

                    if (vocabDto.TopicIds != null && vocabDto.TopicIds.Any())
                    {
                        foreach (var topicId in vocabDto.TopicIds)
                        {
                            var vocabTopic = new VocabularyTopic
                            {
                                VocabularyId = vocabulary.VocabularyId,
                                TopicId = topicId,
                                CreateBy = currentUserId,
                                CreateDate = DateTime.UtcNow.AddHours(7),
                                Status = VocabularyTopicStatus.Active
                            };
                            await _vocabularyTopicRepository.AddAsync(vocabTopic);
                        }
                    }

                    var successMessage = vocabDto.TopicIds != null && vocabDto.TopicIds.Any()
                        ? $"Tạo vocabulary thành công và thêm vào {vocabDto.TopicIds.Count} topic."
                        : "Tạo vocabulary thành công (chưa thuộc topic nào).";

                    response.Results.Add(new VocabularyCreationResult
                    {
                        Text = vocabDto.Text,
                        Definition = vocabDto.Definition,
                        IsSuccess = true,
                        VocabularyId = vocabulary.VocabularyId,
                        AudioURL = audioUrl,
                        Message = successMessage
                    });
                    response.SuccessCount++;
                }
                catch (Exception ex)
                {
                    response.Results.Add(new VocabularyCreationResult
                    {
                        Text = vocabDto.Text,
                        Definition = vocabDto.Definition,
                        IsSuccess = false,
                        ErrorMessage = $"Lỗi hệ thống: {ex.Message}"
                    });
                    response.FailedCount++;
                }
            }

            await _vocabularyRepository.SaveChangesAsync(cancellationToken);

            if (response.SuccessCount == 0)
            {
                return OperationResult<BulkCreateVocabulariesResponse>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    "Không thể tạo vocabulary nào."
                );
            }

            var message = response.FailedCount > 0
                ? $"Xử lý thành công {response.SuccessCount}/{response.TotalVocabularies} từ vựng. {response.FailedCount} từ thất bại."
                : $"Xử lý thành công tất cả {response.SuccessCount} từ vựng.";

            return OperationResult<BulkCreateVocabulariesResponse>.Success(
                response,
                201,
                message
            );
        }
    }
}
