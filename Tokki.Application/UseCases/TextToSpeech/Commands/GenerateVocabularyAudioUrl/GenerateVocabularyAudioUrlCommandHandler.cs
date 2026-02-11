using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TextToSpeech.DTOs;

namespace Tokki.Application.UseCases.TextToSpeech.Commands.GenerateVocabularyAudioUrl
{
    public class GenerateVocabularyAudioUrlCommandHandler
        : IRequestHandler<GenerateVocabularyAudioUrlCommand, OperationResult<TextToSpeechUrlResponse>>
    {
        private readonly ISpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<GenerateVocabularyAudioUrlCommandHandler> _logger;

        public GenerateVocabularyAudioUrlCommandHandler(
            ISpeechService ttsService,
            ICloudinaryService cloudinaryService,
            ILogger<GenerateVocabularyAudioUrlCommandHandler> logger)
        {
            _ttsService = ttsService;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        public async Task<OperationResult<TextToSpeechUrlResponse>> Handle(
            GenerateVocabularyAudioUrlCommand request,
            CancellationToken cancellationToken)
        {
            var text = request.Text?.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                return OperationResult<TextToSpeechUrlResponse>.Failure(
                    new List<Error> { new Error("TTS.InvalidText", "Text không được để trống.") },
                    400,
                    "Text không hợp lệ."
                );
            }

            try
            {
                // 1) TTS -> bytes
                var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(text);

                // 2) Upload -> url
                var folderName = "tokki/vocab-audio";
                var fileName = $"VOCAB_{Guid.NewGuid()}";
                var audioUrl = await _cloudinaryService.UploadAudioAsync(audioBytes, fileName, folderName);

                var response = new TextToSpeechUrlResponse { AudioUrl = audioUrl };

                return OperationResult<TextToSpeechUrlResponse>.Success(response, 200, "Tạo audio URL thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo audio URL cho text: {Text}", text);

                return OperationResult<TextToSpeechUrlResponse>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    $"Lỗi hệ thống: {ex.Message}"
                );
            }
        }
    }
}
