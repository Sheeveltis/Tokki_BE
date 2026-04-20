using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Commands.UpdateVocabulary
{
    public class UpdateVocabularyCommandHandler
        : IRequestHandler<UpdateVocabularyCommand, OperationResult<VocabularyResponseDto>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly ISpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UpdateVocabularyCommandHandler> _logger;

        public UpdateVocabularyCommandHandler(
            IVocabularyRepository vocabularyRepository,
            ISpeechService ttsService,
            ICloudinaryService cloudinaryService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UpdateVocabularyCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
            _ttsService = ttsService;
            _cloudinaryService = cloudinaryService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<VocabularyResponseDto>> Handle(
            UpdateVocabularyCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return OperationResult<VocabularyResponseDto>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            if (request.UpdateData == null)
            {
                return OperationResult<VocabularyResponseDto>.Failure(
                    AppErrors.BadRequest,
                    400,
                    "Thông tin cập nhật không được để trống."
                );
            }

            // Bắt buộc: load kèm children để cascade
            var vocabulary = await _vocabularyRepository.GetByIdWithChildrenAsync(request.VocabularyId);
            if (vocabulary == null)
            {
                return OperationResult<VocabularyResponseDto>.Failure(
                    new List<Error> { AppErrors.VocabularyNotFound },
                    404,
                    AppErrors.VocabularyNotFound.Description
                );
            }

            // Nếu đã Deleted thì không cho update/khôi phục nữa
            if (vocabulary.Status == VocabularyStatus.Deleted)
            {
                return OperationResult<VocabularyResponseDto>.Failure(
                    new List<Error> { AppErrors.VocabularyDeletedCannotUpdate },
                    409,
                    AppErrors.VocabularyDeletedCannotUpdate.Description
                );
            }

            bool textChanged = false;

            // Update Text
            if (!string.IsNullOrWhiteSpace(request.UpdateData.Text))
            {
                var newText = request.UpdateData.Text.Trim();
                if (!string.Equals(vocabulary.Text, newText, StringComparison.Ordinal))
                {
                    vocabulary.Text = newText;
                    textChanged = true;
                }
            }

            // Update Pronunciation
            if (!string.IsNullOrWhiteSpace(request.UpdateData.Pronunciation))
            {
                vocabulary.Pronunciation = request.UpdateData.Pronunciation.Trim();
            }

            // Update Definition
            if (!string.IsNullOrWhiteSpace(request.UpdateData.Definition))
            {
                vocabulary.Definition = request.UpdateData.Definition.Trim();
            }

            // Update ImgURL
            if (!string.IsNullOrWhiteSpace(request.UpdateData.ImgURL))
            {
                vocabulary.ImgURL = request.UpdateData.ImgURL.Trim();
            }

            // Update Status (bỏ qua nếu null)
            if (request.UpdateData.Status.HasValue)
            {
                var newStatus = request.UpdateData.Status.Value;

                if (newStatus != vocabulary.Status)
                {
                    vocabulary.Status = newStatus;

                    // 1) Vocab -> Deleted: cascade cả Topics và Examples -> Deleted
                    if (newStatus == VocabularyStatus.Deleted)
                    {
                        CascadeTopicsToDeleted(vocabulary, currentUserId);
                        CascadeExamplesToDeleted(vocabulary);
                    }
                    // 2) Vocab -> Draft: cascade cả Topics và Examples -> Draft
                    else if (newStatus == VocabularyStatus.Draft)
                    {
                        CascadeTopicsToDraft(vocabulary, currentUserId);
                        CascadeExamplesToDraft(vocabulary);
                    }
                    // 3) Vocab -> Active: cascade cả Topics và Examples -> Active
                    else if (newStatus == VocabularyStatus.Active)
                    {
                        CascadeTopicsToActive(vocabulary, currentUserId);
                        CascadeExamplesToActive(vocabulary);
                    }
                }
            }

            // Update Topics (Replace existing topics with new list)
            if (request.UpdateData.TopicIds != null)
            {
                vocabulary.VocabularyTopics.Clear();

                foreach (var topicId in request.UpdateData.TopicIds)
                {
                    vocabulary.VocabularyTopics.Add(new Tokki.Domain.Entities.VocabularyTopic
                    {
                        VocabularyId = vocabulary.VocabularyId,
                        TopicId = topicId,
                        Status = VocabularyTopicStatus.Active,
                        CreateBy = currentUserId,
                        CreateDate = DateTime.UtcNow.AddHours(7),
                        UpdateBy = currentUserId,
                        UpdateDate = DateTime.UtcNow.AddHours(7)
                    });
                }
            }

            // Regenerate audio nếu Text đổi
            if (textChanged)
            {
                try
                {
                    var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(vocabulary.Text);
                    var folderName = "tokki/vocab-audio";
                    var fileName = $"VOCAB_{Guid.NewGuid()}";
                    var audioUrl = await _cloudinaryService.UploadAudioAsync(audioBytes, fileName, folderName);
                    vocabulary.AudioURL = audioUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể tạo audio cho vocabulary: {Text}", vocabulary.Text);
                }
            }

            // Audit vocab
            vocabulary.UpdateBy = currentUserId;
            vocabulary.UpdateDate = DateTime.UtcNow.AddHours(7);

            try
            {
                await _vocabularyRepository.UpdateAsync(vocabulary);
                await _vocabularyRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vocabulary: {VocabularyId}", request.VocabularyId);
                return OperationResult<VocabularyResponseDto>.Failure(
                    new List<Error> { new Error("App.ServerError", ex.Message) },
                    500,
                    ex.Message
                );
            }

            var response = new VocabularyResponseDto
            {
                VocabularyId = vocabulary.VocabularyId,
                Text = vocabulary.Text,
                Definition = vocabulary.Definition,
                Pronunciation = vocabulary.Pronunciation
            };

            return OperationResult<VocabularyResponseDto>.Success(
                response,
                200,
                $"Cập nhật vocabulary '{vocabulary.Text}' thành công."
            );
        }

        private static void CascadeTopicsToDeleted(Tokki.Domain.Entities.Vocabulary vocabulary, string userId)
        {
            foreach (var vt in vocabulary.VocabularyTopics)
            {
                vt.Status = VocabularyTopicStatus.Deleted;
                vt.UpdateBy = userId;
                vt.UpdateDate = DateTime.UtcNow.AddHours(7);
            }
        }

        private static void CascadeTopicsToDraft(Tokki.Domain.Entities.Vocabulary vocabulary, string userId)
        {
            foreach (var vt in vocabulary.VocabularyTopics)
            {
                vt.Status = VocabularyTopicStatus.Draft;
                vt.UpdateBy = userId;
                vt.UpdateDate = DateTime.UtcNow.AddHours(7);
            }
        }

        private static void CascadeTopicsToActive(Tokki.Domain.Entities.Vocabulary vocabulary, string userId)
        {
            foreach (var vt in vocabulary.VocabularyTopics)
            {
                vt.Status = VocabularyTopicStatus.Active;
                vt.UpdateBy = userId;
                vt.UpdateDate = DateTime.UtcNow.AddHours(7);
            }
        }

        private static void CascadeExamplesToDeleted(Tokki.Domain.Entities.Vocabulary vocabulary)
        {
            foreach (var ex in vocabulary.VocabularyExamples)
            {
                ex.Status = VocabularyExampleStatus.Deleted;
            }
        }

        private static void CascadeExamplesToDraft(Tokki.Domain.Entities.Vocabulary vocabulary)
        {
            foreach (var ex in vocabulary.VocabularyExamples)
            {
                ex.Status = VocabularyExampleStatus.Draft;
            }
        }

        private static void CascadeExamplesToActive(Tokki.Domain.Entities.Vocabulary vocabulary)
        {
            foreach (var ex in vocabulary.VocabularyExamples)
            {
                ex.Status = VocabularyExampleStatus.Active;
            }
        }
    }
}