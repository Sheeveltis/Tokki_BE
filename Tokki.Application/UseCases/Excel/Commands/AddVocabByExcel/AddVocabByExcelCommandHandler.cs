using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.Commands.AddVocabByExcel;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate
{
    public class AddVocabByExcelCommandHandler : IRequestHandler<AddVocabByExcelCommand, OperationResult<ImportVocabularyResponse>>
    {
        private readonly IExcelService _excelService;
        private readonly IVocabularyRepository _vocabRepo;
        private readonly IVocabularyTopicRepository _vocabTopicRepo;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<AddVocabByExcelCommandHandler> _logger;
        private readonly ITextToSpeechService _ttsService;
        private readonly IIdGeneratorService _idGenerator;

        public AddVocabByExcelCommandHandler(
            IExcelService excelService,
            IVocabularyRepository vocabRepo,
            ICloudinaryService cloudinaryService,
            ILogger<AddVocabByExcelCommandHandler> logger,
            ITextToSpeechService ttsService,
            IIdGeneratorService idGenerator,
            IVocabularyTopicRepository vocabTopicRepo)
        {
            _excelService = excelService;
            _vocabRepo = vocabRepo;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
            _ttsService = ttsService;
            _idGenerator = idGenerator;
            _vocabTopicRepo = vocabTopicRepo;
        }

        public async Task<OperationResult<ImportVocabularyResponse>> Handle(AddVocabByExcelCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Start ImportVocabulary. StaffId: {StaffId}, TopicId: {TopicId}, File: {FileName}",
                request.StaffId, request.TopicId, request.File.FileName);

            var response = new ImportVocabularyResponse();

            var extractedVocabs = await _excelService.ExtractVocabularyDataAsync(request.File);
            if (extractedVocabs == null || !extractedVocabs.Any())
            {
                return OperationResult<ImportVocabularyResponse>.Failure(AppErrors.ExcelNoValidDataFound);
            }

            var vocabsToCheck = extractedVocabs.Select(x => (x.Text, x.Definition)).ToList();
            var existingEntities = await _vocabRepo.GetExistingVocabEntitiesAsync(vocabsToCheck);
            var existingVocabIdsInTopic = new HashSet<string>(); 
            if (!string.IsNullOrEmpty(request.TopicId))
            {
                var ids = await _vocabTopicRepo.GetVocabIdsByTopicIdAsync(request.TopicId);
                existingVocabIdsInTopic = new HashSet<string>(ids);
            }
            var newItemsToProcess = new List<VocabularyExcelDTO>();
            var finalVocabsForTopic = new List<Domain.Entities.Vocabulary>();

            foreach (var item in extractedVocabs)
            {
                var existingMatch = existingEntities.FirstOrDefault(e =>
                    e.Text.Equals(item.Text, StringComparison.OrdinalIgnoreCase) &&
                    e.Definition.Equals(item.Definition, StringComparison.OrdinalIgnoreCase));

                if (existingMatch != null)
                {
                    if (!string.IsNullOrEmpty(request.TopicId))
                    {
                        if (existingVocabIdsInTopic.Contains(existingMatch.VocabularyId))
                        {
                            response.FailureList.Add(new VocabularyPreviewDTO
                            {
                                Text = item.Text,
                                Definition = item.Definition,
                                Reason = "Từ vựng này đã tồn tại trong Topic rồi." 
                            });
                        }
                        else
                        {
                            finalVocabsForTopic.Add(existingMatch);
                            response.SuccessList.Add(new VocabularyPreviewDTO
                            {
                                Text = existingMatch.Text,
                                Definition = existingMatch.Definition,
                                Pronunciation = existingMatch.Pronunciation,
                                ImageUrl = existingMatch.ImgURL,
                                Reason = "Đã có sẵn trong từ điển -> Sẽ thêm vào Topic"
                            });
                        }
                    }
                    else
                    {
                        response.FailureList.Add(new VocabularyPreviewDTO
                        {
                            Text = item.Text,
                            Definition = item.Definition,
                            Reason = "Từ vựng và nghĩa này đã tồn tại trong hệ thống."
                        });
                    }
                }
                else
                {
                    bool isDuplicateInFile = newItemsToProcess.Any(n =>
                        n.Text.Equals(item.Text, StringComparison.OrdinalIgnoreCase) &&
                        n.Definition.Equals(item.Definition, StringComparison.OrdinalIgnoreCase));

                    if (isDuplicateInFile)
                    {
                        response.FailureList.Add(new VocabularyPreviewDTO
                        {
                            Text = item.Text,
                            Definition = item.Definition,
                            Reason = "Từ vựng này bị lặp lại nhiều lần trong file Excel."
                        });
                    }
                    else
                    {
                        newItemsToProcess.Add(item);
                    }
                }
            }
            if (!newItemsToProcess.Any() && !finalVocabsForTopic.Any())
            {
                return OperationResult<ImportVocabularyResponse>.Success(response, 200, "Không có thao tác nào được thực hiện.");
            }

            var newlyCreatedEntities = new List<Domain.Entities.Vocabulary>();
            const string CLOUDINARY_FOLDER = "tokki/vocab-image";
            const string AUDIO_FOLDER = "tokki/vocab-audio";

            foreach (var item in newItemsToProcess)
            {
                //string finalImageUrl = null;
                //if (!string.IsNullOrWhiteSpace(item.ImageUrl) && IsValidUrl(item.ImageUrl))
                //{
                //    try
                //    {
                //        finalImageUrl = await _cloudinaryService.UploadImageFromUrlAsync(item.ImageUrl, CLOUDINARY_FOLDER);
                //    }
                //    catch
                //    {
                //        finalImageUrl = null;
                //    }
                //}

                string? audioUrl = null;
                try
                {
                    var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(item.Text);
                    audioUrl = await _cloudinaryService.UploadAudioAsync(audioBytes, $"VOCAB_{Guid.NewGuid()}", AUDIO_FOLDER);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "TTS Error");
                }

                var entity = new Domain.Entities.Vocabulary
                {
                    VocabularyId = _idGenerator.Generate(15),
                    Text = item.Text,
                    Pronunciation = item.Pronunciation,
                    Definition = item.Definition,
                    ImgURL = item.ImageUrl,
                    AudioURL = audioUrl,
                    CreateBy = request.StaffId,
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    Status = VocabularyStatus.Active
                };

                newlyCreatedEntities.Add(entity);

                response.SuccessList.Add(new VocabularyPreviewDTO
                {
                    Text = entity.Text,
                    Definition = entity.Definition,
                    Pronunciation = entity.Pronunciation,
                    ImageUrl = entity.ImgURL,
                    Reason = "Tạo mới thành công"
                });
            }

            if (newlyCreatedEntities.Any())
            {
                try
                {
                    await _vocabRepo.AddRangeAsync(newlyCreatedEntities);
                    finalVocabsForTopic.AddRange(newlyCreatedEntities);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DB Save Error");
                    return OperationResult<ImportVocabularyResponse>.Failure(new Error("DB_ERROR", "Lỗi khi lưu từ mới."));
                }
            }

            if (!string.IsNullOrEmpty(request.TopicId) && finalVocabsForTopic.Any())
            {
                try
                {
                    var topicResult = await _vocabTopicRepo.AddVocabulariesToTopicWithTransactionAsync(
                        request.TopicId,
                        finalVocabsForTopic,
                        request.StaffId,
                        cancellationToken
                    );

                    _logger.LogInformation("Linked {Count} vocabs to Topic {TopicId}", topicResult.AddedCount, request.TopicId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Topic Link Error");
                }
            }

            return OperationResult<ImportVocabularyResponse>.Success(response, 200,
                $"Xử lý xong. Tạo mới: {newlyCreatedEntities.Count}. Tổng Success: {response.SuccessList.Count}");
        }

        private bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            return url.Trim().StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }
    }
}