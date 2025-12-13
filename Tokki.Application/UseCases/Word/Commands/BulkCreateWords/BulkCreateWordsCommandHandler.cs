using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Word.Commands.BulkCreateWords;
using Tokki.Application.UseCases.Word.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Words.Commands.BulkCreateWords
{
    public class BulkCreateWordsCommandHandler : IRequestHandler<BulkCreateWordsCommand, OperationResult<BulkCreateWordsResponse>>
    {
        private readonly IWordRepository _wordRepository;
        private readonly IMeaningRepository _meaningRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly IMeaningTopicRepository _meaningTopicRepository;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ITextToSpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BulkCreateWordsCommandHandler(
            IWordRepository wordRepository,
            IMeaningRepository meaningRepository,
            ITopicRepository topicRepository,
            IMeaningTopicRepository meaningTopicRepository,
            IIdGeneratorService idGenerator,
            ITextToSpeechService ttsService,
            ICloudinaryService cloudinaryService,
            IHttpContextAccessor httpContextAccessor)
        {
            _wordRepository = wordRepository;
            _meaningRepository = meaningRepository;
            _topicRepository = topicRepository;
            _meaningTopicRepository = meaningTopicRepository;
            _idGenerator = idGenerator;
            _ttsService = ttsService;
            _cloudinaryService = cloudinaryService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<BulkCreateWordsResponse>> Handle(
            BulkCreateWordsCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("Không xác định được người dùng.");

            // Validate Topic tồn tại
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<BulkCreateWordsResponse>.Failure(
                    new List<Error> { new Error("TOPIC_NOT_FOUND", $"Topic '{request.TopicId}' không tồn tại.") }
                );
            }

            var response = new BulkCreateWordsResponse
            {
                TotalWords = request.Words.Count
            };

            foreach (var wordDto in request.Words)
            {
                try
                {
                    // Kiểm tra từ vựng đã tồn tại chưa
                    var existingWord = await _wordRepository.GetByTextAsync(wordDto.Text);

                    Tokki.Domain.Entities.Word word;
                    bool isNewWord = existingWord == null;

                    if (isNewWord)
                    {
                        // TẠO TỪ MỚI
                        string? audioUrl = null;
                        try
                        {
                            var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(wordDto.Text);
                            string folderName = "tokki/vocab-audio";
                            string fileName = $"WORD_{Guid.NewGuid()}";
                            audioUrl = await _cloudinaryService.UploadAudioAsync(audioBytes, fileName, folderName);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Failed to generate audio for '{wordDto.Text}': {ex.Message}");
                        }

                        word = new Tokki.Domain.Entities.Word
                        {
                            WordId = _idGenerator.Generate(15),
                            Text = wordDto.Text,
                            Pronunciation = wordDto.Pronunciation,
                            AudioURL = audioUrl,
                            CreateBy = currentUserId,
                            CreateDate = DateTime.UtcNow,
                            Status = WordStatus.Active
                        };

                        await _wordRepository.AddAsync(word);
                    }
                    else
                    {
                        // SỬ DỤNG TỪ ĐÃ TỒN TẠI
                        word = existingWord;
                    }

                    // TẠO MEANINGS CHO TỪ (dù từ mới hay cũ)
                    int addedMeaningsCount = 0;
                    foreach (var meaningDto in wordDto.Meanings)
                    {
                        // Kiểm tra xem nghĩa này đã tồn tại trong topic này chưa
                        var existingMeaningInTopic = await _meaningRepository
                            .GetMeaningByDefinitionAndTopicAsync(word.WordId, meaningDto.Definition, request.TopicId);

                        if (existingMeaningInTopic != null)
                        {
                            // Nghĩa này đã tồn tại trong topic này, bỏ qua
                            continue;
                        }

                        // Tạo Meaning mới
                        var meaning = new Meaning
                        {
                            MeaningId = _idGenerator.Generate(15),
                            WordId = word.WordId,
                            Definition = meaningDto.Definition,
                            ExampleSentence = meaningDto.ExampleSentence,
                            ImgURL = meaningDto.ImgURL,
                            CreateBy = currentUserId,
                            CreateDate = DateTime.UtcNow,
                            Status = MeaningStatus.Active
                        };

                        await _meaningRepository.AddAsync(meaning);

                        // Tạo MeaningTopic relationship với TopicId từ command
                        var meaningTopic = new MeaningTopic
                        {
                            MeaningId = meaning.MeaningId,
                            TopicId = request.TopicId,
                            CreateBy = currentUserId,
                            CreateDate = DateTime.UtcNow,
                            Status = MeaningTopicStatus.Active
                        };

                        await _meaningTopicRepository.AddAsync(meaningTopic);
                        addedMeaningsCount++;
                    }

                    string resultMessage = isNewWord
                        ? $"Tạo từ mới với {addedMeaningsCount} nghĩa"
                        : $"Thêm {addedMeaningsCount} nghĩa mới vào từ đã tồn tại";

                    response.Results.Add(new WordCreationResult
                    {
                        Text = wordDto.Text,
                        IsSuccess = true,
                        WordId = word.WordId,
                    });
                    response.SuccessCount++;
                }
                catch (Exception ex)
                {
                    response.Results.Add(new WordCreationResult
                    {
                        Text = wordDto.Text,
                        IsSuccess = false,
                        ErrorMessage = $"Lỗi hệ thống: {ex.Message}"
                    });
                    response.FailedCount++;
                }
            }

            // Lưu tất cả thay đổi
            await _wordRepository.SaveChangesAsync(cancellationToken);

            if (response.SuccessCount == 0)
            {
                return OperationResult<BulkCreateWordsResponse>.Failure(
                    new List<Error> { AppErrors.ServerError }
                );
            }

            var message = response.FailedCount > 0
                ? $"Xử lý thành công {response.SuccessCount}/{response.TotalWords} từ vựng trong topic '{topic.TopicName}'."
                : $"Xử lý thành công tất cả {response.SuccessCount} từ vựng trong topic '{topic.TopicName}'.";

            return OperationResult<BulkCreateWordsResponse>.Success(
                response,
                201,
                message
            );
        }
    }

}