using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.Commands.AddVocabByExcel;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Excel.Commands.AddVocabByExcel
{
    public class AddVocabByExcelCommandHandler : IRequestHandler<AddVocabByExcelCommand, OperationResult<ImportVocabularyResponse>>
    {
        private readonly IExcelService _excelService;
        private readonly IVocabularyRepository _vocabRepo;
        private readonly IVocabularyTopicRepository _vocabTopicRepo;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<AddVocabByExcelCommandHandler> _logger;
        private readonly ISpeechService _ttsService;
        private readonly IIdGeneratorService _idGenerator;

        public AddVocabByExcelCommandHandler(
            IExcelService excelService,
            IVocabularyRepository vocabRepo,
            ICloudinaryService cloudinaryService,
            ILogger<AddVocabByExcelCommandHandler> logger,
            ISpeechService ttsService,
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
            var response = new ImportVocabularyResponse();
            try
            {
                _logger.LogInformation("Start ImportVocabulary. StaffId: {StaffId}, TopicId: {TopicId}, File: {FileName}",
                    request.StaffId, request.TopicId, request.File.FileName);

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
                        // Check if this vocab is already picked earlier in THIS excel file
                        if (finalVocabsForTopic.Any(v => v.VocabularyId == existingMatch.VocabularyId)) 
                        {
                            response.FailureList.Add(new VocabularyPreviewDTO
                            {
                                Text = item.Text,
                                Definition = item.Definition,
                                Reason = "Từ vựng này bị lặp lại trong file Excel."
                            });
                            continue;
                        }

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

                                response.LinkedExistingVocabList.Add(new VocabularyPreviewDTO
                                {
                                    Text = existingMatch.Text,
                                    Definition = existingMatch.Definition,
                                    Pronunciation = existingMatch.Pronunciation,
                                    ImageUrl = existingMatch.ImgURL,
                                    Reason = "Đã có sẵn trong từ điển -> Đã thêm vào Topic"
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

                var newlyCreatedEntities = new System.Collections.Concurrent.ConcurrentBag<Domain.Entities.Vocabulary>();
                const string CLOUDINARY_FOLDER = "tokki/vocab-image";
                const string AUDIO_FOLDER = "tokki/vocab-audio";

                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = cancellationToken };

                await Parallel.ForEachAsync(newItemsToProcess, parallelOptions, async (item, token) =>
                {
                    try
                    {
                        string? finalImageUrl = null;
                        if (!string.IsNullOrWhiteSpace(item.ImageUrl) && Uri.TryCreate(item.ImageUrl, UriKind.Absolute, out _))
                        {
                            finalImageUrl = await _cloudinaryService.UploadImageFromUrlAsync(item.ImageUrl, CLOUDINARY_FOLDER);
                        }

                        string? audioUrl = null;
                        var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(item.Text);
                        audioUrl = await _cloudinaryService.UploadAudioAsync(audioBytes, $"VOCAB_{_idGenerator.Generate(8)}", AUDIO_FOLDER);

                        var entity = new Domain.Entities.Vocabulary
                        {
                            VocabularyId = _idGenerator.Generate(15),
                            Text = item.Text,
                            Pronunciation = item.Pronunciation,
                            Definition = item.Definition,
                            ImgURL = finalImageUrl, 
                            AudioURL = audioUrl,
                            CreateBy = request.StaffId,
                            CreateDate = DateTime.UtcNow.AddHours(7), 
                            Status = VocabularyStatus.Active
                        };

                        newlyCreatedEntities.Add(entity);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Lỗi tại từ vựng '{item.Text}': {ex.Message}", ex);
                    }
                });

                foreach (var entity in newlyCreatedEntities.OrderBy(e => e.Text))
                {
                    response.AddedNewVocabList.Add(new VocabularyPreviewDTO
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
                    var entitiesList = newlyCreatedEntities.ToList();
                    await _vocabRepo.AddRangeAsync(entitiesList);
                    finalVocabsForTopic.AddRange(entitiesList);
                }

                int actualLinked = 0;
                if (!string.IsNullOrEmpty(request.TopicId) && finalVocabsForTopic.Any())
                {
                    // Đảm bảo không có ID trùng lặp 
                    var uniqueVocabs = finalVocabsForTopic
                        .GroupBy(v => v.VocabularyId)
                        .Select(g => g.First())
                        .ToList();

                    var topicResult = await _vocabTopicRepo.AddOrReactivateVocabulariesToTopicAsync(
                        request.TopicId,
                        uniqueVocabs,
                        request.StaffId,
                        cancellationToken
                    );

                    // Lấy số lượng thực tế mà Repo đã xử lý thành công
                    actualLinked = topicResult.AddedOrReactivated + topicResult.SkippedAlreadyActive;
                }

                // Câu thông báo dựa trên số lượng THỰC TẾ từ Database trả về
                var msg = $"Xử lý xong. Thêm mới vào hệ thống: {response.AddedNewCount}. Đã vào Topic: {actualLinked}. Lỗi: {response.FailureCount}";
                
                return OperationResult<ImportVocabularyResponse>.Success(response, 200, msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ImportVocabulary Error.");
                var errorMsg = $"Đã có lỗi (Lỗi: {ex.Message}). Quá trình đã dừng lại.";
                return OperationResult<ImportVocabularyResponse>.Success(response, 400, errorMsg);
            }
        }
    }
}