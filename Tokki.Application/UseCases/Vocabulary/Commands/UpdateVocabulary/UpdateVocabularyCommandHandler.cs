using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Commands.UpdateVocabulary
{
    public class UpdateVocabularyCommandHandler : IRequestHandler<UpdateVocabularyCommand, OperationResult<VocabularyResponseDto>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly ITextToSpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UpdateVocabularyCommandHandler> _logger;

        public UpdateVocabularyCommandHandler(
            IVocabularyRepository vocabularyRepository,
            ITextToSpeechService ttsService,
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

            bool textChanged = false;

            // 3. CẬP NHẬT TEXT (chỉ khi không null và không rỗng)
            if (!string.IsNullOrWhiteSpace(request.UpdateData.Text))
            {
                if (vocabulary.Text != request.UpdateData.Text)
                {
                    vocabulary.Text = request.UpdateData.Text;
                    textChanged = true;
                }
            }

            // 4. CẬP NHẬT CÁC TRƯỜNG CƠ BẢN (chỉ khi không null và không rỗng)
            if (!string.IsNullOrWhiteSpace(request.UpdateData.Pronunciation))
            {
                vocabulary.Pronunciation = request.UpdateData.Pronunciation;
            }

            if (!string.IsNullOrWhiteSpace(request.UpdateData.Definition))
            {
                vocabulary.Definition = request.UpdateData.Definition;
            }

<<<<<<< HEAD
            if (request.UpdateData.ImgURL != null)
=======
            //if (!string.IsNullOrWhiteSpace(request.UpdateData.ExampleSentence))
            //{
            //    vocabulary.ExampleSentence = request.UpdateData.ExampleSentence;
            //}

            if (!string.IsNullOrWhiteSpace(request.UpdateData.ImgURL))
>>>>>>> 519bc38f4c1de86d626062dd3e0674f2cf6e5803
            {
                vocabulary.ImgURL = request.UpdateData.ImgURL;
            }

            // 5. CẬP NHẬT AUDIO URL NẾU TEXT THAY ĐỔI
            if (textChanged)
            {
                try
                {
                    var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(vocabulary.Text);
                    string folderName = "tokki/vocab-audio";
                    string fileName = $"VOCAB_{Guid.NewGuid()}";
                    var audioUrl = await _cloudinaryService.UploadAudioAsync(audioBytes, fileName, folderName);
                    vocabulary.AudioURL = audioUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể tạo audio cho vocabulary: {Text}", vocabulary.Text);
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
                _logger.LogError(ex, "Error updating vocabulary: {VocabularyId}", request.VocabularyId);
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
                Pronunciation = vocabulary.Pronunciation
            };

            return OperationResult<VocabularyResponseDto>.Success(
                response,
                200,
                $"Cập nhật vocabulary '{vocabulary.Text}' thành công."
            );
        }
    }
}