using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Word.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Word.Commands.UpdateWord
{
    public class UpdateWordCommandHandler : IRequestHandler<UpdateWordCommand, OperationResult<WordResponseDto>>
    {
        private readonly IWordRepository _wordRepository;
        private readonly IMeaningRepository _meaningRepository;
        private readonly ITextToSpeechService _ttsService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateWordCommandHandler(
            IWordRepository wordRepository,
            IMeaningRepository meaningRepository,
            ITextToSpeechService ttsService,
            ICloudinaryService cloudinaryService,
            IIdGeneratorService idGenerator,
            IHttpContextAccessor httpContextAccessor)
        {
            _wordRepository = wordRepository;
            _meaningRepository = meaningRepository;
            _ttsService = ttsService;
            _cloudinaryService = cloudinaryService;
            _idGenerator = idGenerator;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<WordResponseDto>> Handle(
            UpdateWordCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("Không xác định được người dùng.");

            // Lấy Word hiện tại
            var word = await _wordRepository.GetByIdAsync(request.WordId);
            if (word == null)
            {
                return OperationResult<WordResponseDto>.Failure(
                    new List<Error> { AppErrors.WordNotFound }
                );
            }

            // Cập nhật Text nếu có
            if (!string.IsNullOrEmpty(request.Text) && request.Text != word.Text)
            {
                // Kiểm tra trùng lặp
                var existingWord = await _wordRepository.GetByTextAsync(request.Text);
                if (existingWord != null && existingWord.WordId != word.WordId)
                {
                    return OperationResult<WordResponseDto>.Failure(
                        new List<Error> { AppErrors.WordDuplicated }
                    );
                }

                word.Text = request.Text;

                // Tạo lại audio cho text mới
                try
                {
                    var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(request.Text);
                    string folderName = "tokki/vocab-audio";
                    string fileName = $"WORD_{Guid.NewGuid()}";
                    word.AudioURL = await _cloudinaryService.UploadAudioAsync(audioBytes, fileName, folderName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to generate audio for '{request.Text}': {ex.Message}");
                }
            }

            // Cập nhật Pronunciation nếu có
            if (request.Pronunciation != null)
            {
                word.Pronunciation = request.Pronunciation;
            }

            // Cập nhật thông tin chung
            word.UpdateBy = currentUserId;
            word.UpdateDate = DateTime.UtcNow;

            await _wordRepository.UpdateAsync(word);

            // Cập nhật Meanings nếu có
            if (request.Meanings != null && request.Meanings.Any())
            {
                foreach (var meaningDto in request.Meanings)
                {
                    if (!string.IsNullOrEmpty(meaningDto.MeaningId))
                    {
                        // Cập nhật meaning hiện tại
                        var meaning = await _meaningRepository.GetByIdAsync(meaningDto.MeaningId);
                        if (meaning != null && meaning.WordId == word.WordId)
                        {
                            meaning.Definition = meaningDto.Definition;
                            meaning.ExampleSentence = meaningDto.ExampleSentence;
                            meaning.ImgURL = meaningDto.ImgURL;
                            meaning.UpdateBy = currentUserId;
                            meaning.UpdateDate = DateTime.UtcNow;

                            await _meaningRepository.UpdateAsync(meaning);
                        }
                    }
                    else
                    {
                        // Thêm meaning mới
                        var newMeaning = new Meaning
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

                        await _meaningRepository.AddAsync(newMeaning);
                    }
                }
            }

            await _wordRepository.SaveChangesAsync(cancellationToken);

            var response = new WordResponseDto
            {
                WordId = word.WordId,
                Text = word.Text,
                Pronunciation = word.Pronunciation,
                AudioURL = word.AudioURL
            };

            return OperationResult<WordResponseDto>.Success(
                response,
                200,
                "Cập nhật từ vựng thành công."
            );
        }
    }
}
