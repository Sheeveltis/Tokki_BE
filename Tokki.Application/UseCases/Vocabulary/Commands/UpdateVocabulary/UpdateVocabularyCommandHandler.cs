using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Commands.UpdateVocabulary
{
    public class UpdateVocabularyCommandHandler : IRequestHandler<UpdateVocabularyCommand, OperationResult<VocabularyResponseDto>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;
        private readonly ITextToSpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateVocabularyCommandHandler(
            IVocabularyRepository vocabularyRepository,
            ITopicRepository topicRepository,
            IVocabularyTopicRepository vocabularyTopicRepository,
            ITextToSpeechService ttsService,
            ICloudinaryService cloudinaryService,
            IHttpContextAccessor httpContextAccessor)
        {
            _vocabularyRepository = vocabularyRepository;
            _topicRepository = topicRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
            _ttsService = ttsService;
            _cloudinaryService = cloudinaryService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<VocabularyResponseDto>> Handle(
            UpdateVocabularyCommand request,
            CancellationToken cancellationToken)
        {
            // 1. XÁC THỰC NGƯỜI DÙNG
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return OperationResult<VocabularyResponseDto>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401
                );
            }

            // 2. TÌM VOCABULARY
            var vocabulary = await _vocabularyRepository.GetByIdAsync(request.VocabularyId);
            if (vocabulary == null)
            {
                return OperationResult<VocabularyResponseDto>.Failure(
                    new List<Error> { AppErrors.VocabularyNotFound },
                    404
                );
            }

            // 3. CẬP NHẬT TEXT VÀ TẠO LẠI AUDIO (NẾU TEXT THAY ĐỔI)
            bool textChanged = false;
            if (!string.IsNullOrWhiteSpace(request.UpdateData.Text) &&
                request.UpdateData.Text != vocabulary.Text)
            {
                vocabulary.Text = request.UpdateData.Text;
                textChanged = true;

                // Tự động tạo lại audio cho text mới
                try
                {
                    var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(request.UpdateData.Text);
                    string folderName = "tokki/vocab-audio"; // KHÔNG ĐƯỢC THAY ĐỔI
                    string fileName = $"VOCAB_{Guid.NewGuid()}";
                    var newAudioUrl = await _cloudinaryService.UploadAudioAsync(audioBytes, fileName, folderName);

                    vocabulary.AudioURL = newAudioUrl;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to generate audio for updated text '{request.UpdateData.Text}': {ex.Message}");

                    // Trả về lỗi nếu việc tạo audio thất bại
                    return OperationResult<VocabularyResponseDto>.Failure(
                        new List<Error> { AppErrors.AudioGenerationFailed },
                        500
                    );
                }
            }

            // 4. CẬP NHẬT CÁC TRƯỜNG CƠ BẢN
            if (request.UpdateData.Pronunciation != null)
            {
                vocabulary.Pronunciation = request.UpdateData.Pronunciation;
            }

            if (request.UpdateData.Definition != null)
            {
                vocabulary.Definition = request.UpdateData.Definition;
            }

            if (request.UpdateData.ExampleSentence != null)
            {
                vocabulary.ExampleSentence = request.UpdateData.ExampleSentence;
            }

            if (request.UpdateData.ImgURL != null)
            {
                vocabulary.ImgURL = request.UpdateData.ImgURL;
            }

            // 5. CẬP NHẬT TOPICS (NẾU ĐƯỢC CUNG CẤP)
            if (request.UpdateData.TopicIds != null)
            {
                // Validate tất cả topics tồn tại
                if (request.UpdateData.TopicIds.Any())
                {
                    var invalidTopics = new List<string>();
                    foreach (var topicId in request.UpdateData.TopicIds)
                    {
                        var topic = await _topicRepository.GetByIdAsync(topicId);
                        if (topic == null)
                        {
                            invalidTopics.Add(topicId);
                        }
                    }

                    if (invalidTopics.Any())
                    {
                        return OperationResult<VocabularyResponseDto>.Failure(
                            new List<Error> { AppErrors.TopicNotFound },
                            404
                        );
                    }
                }

                // Xóa tất cả vocabulary-topic relationships cũ
                var existingVocabTopics = await _vocabularyTopicRepository
                    .GetByVocabularyIdAsync(vocabulary.VocabularyId);

                foreach (var vt in existingVocabTopics)
                {
                    await _vocabularyTopicRepository.DeleteAsync(vt);
                }

                // Tạo relationships mới (nếu TopicIds không rỗng)
                if (request.UpdateData.TopicIds.Any())
                {
                    foreach (var topicId in request.UpdateData.TopicIds)
                    {
                        var vocabTopic = new VocabularyTopic
                        {
                            VocabularyId = vocabulary.VocabularyId,
                            TopicId = topicId,
                            CreateBy = currentUserId,
                            CreateDate = DateTime.UtcNow,
                            Status = VocabularyTopicStatus.Active
                        };
                        await _vocabularyTopicRepository.AddAsync(vocabTopic);
                    }
                }
            }

            // 6. CẬP NHẬT AUDIT FIELDS
            vocabulary.UpdateBy = currentUserId;
            vocabulary.UpdateDate = DateTime.UtcNow;

            try
            {
                await _vocabularyRepository.UpdateAsync(vocabulary);
                await _vocabularyRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating vocabulary: {ex.Message}");
                return OperationResult<VocabularyResponseDto>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500
                );
            }

            // 7. TRẢ VỀ RESPONSE
            var response = new VocabularyResponseDto
            {
                VocabularyId = vocabulary.VocabularyId,
                Text = vocabulary.Text,
                Definition = vocabulary.Definition,
                Pronunciation = vocabulary.Pronunciation,
                AudioURL = vocabulary.AudioURL
            };

            var message = textChanged
                ? $"Cập nhật vocabulary '{vocabulary.Text}' thành công (đã tạo lại audio)."
                : $"Cập nhật vocabulary '{vocabulary.Text}' thành công.";

            return OperationResult<VocabularyResponseDto>.Success(
                response,
                200,
                message
            );
        }
    }
}