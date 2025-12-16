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
        private readonly IIdGeneratorService _idGenerator;
        private readonly ITextToSpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<BulkCreateVocabulariesCommandHandler> _logger;

        public BulkCreateVocabulariesCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IIdGeneratorService idGenerator,
            ITextToSpeechService ttsService,
            ICloudinaryService cloudinaryService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<BulkCreateVocabulariesCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
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
                    // 2. Kiểm tra vocabulary đã tồn tại chưa
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

                        response.Results.Add(new VocabularyCreationResult
                        {
                            Text = vocabDto.Text,
                            Definition = vocabDto.Definition,
                            IsSuccess = true,
                            VocabularyId = existingVocab.VocabularyId,
                            Message = "Vocabulary đã tồn tại."
                        });
                        response.SuccessCount++;
                        continue;
                    }

                    // 3. Tạo audio URL
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

                    // 4. Tạo vocabulary mới
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

                    response.Results.Add(new VocabularyCreationResult
                    {
                        Text = vocabDto.Text,
                        Definition = vocabDto.Definition,
                        IsSuccess = true,
                        VocabularyId = vocabulary.VocabularyId,
                        AudioURL = audioUrl,
                        Message = "Tạo vocabulary thành công."
                    });
                    response.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi tạo vocabulary: {Text}", vocabDto.Text);
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