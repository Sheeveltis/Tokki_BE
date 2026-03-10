using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Excel.Commands.ImportPronunciationExample
{
    public class ImportPronunciationExampleCommandHandler : IRequestHandler<ImportPronunciationExampleCommand, OperationResult<ImportExampleResponse>>
    {
        private readonly IExcelService _excelService;
        private readonly IPronunciationExampleRepository _exampleRepo;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ISpeechService _ttsService;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ILogger<ImportPronunciationExampleCommandHandler> _logger;

        public ImportPronunciationExampleCommandHandler(
            IExcelService excelService,
            IPronunciationExampleRepository exampleRepo,
            ICloudinaryService cloudinaryService,
            ISpeechService ttsService,
            IIdGeneratorService idGenerator,
            ILogger<ImportPronunciationExampleCommandHandler> logger)
        {
            _excelService = excelService;
            _exampleRepo = exampleRepo;
            _cloudinaryService = cloudinaryService;
            _ttsService = ttsService;
            _idGenerator = idGenerator;
            _logger = logger;
        }

        public async Task<OperationResult<ImportExampleResponse>> Handle(ImportPronunciationExampleCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bắt đầu ImportPronunciationExample. UserId: {UserId}, File: {FileName}",
                request.UserId, request.File.FileName);

            var response = new ImportExampleResponse();
            const string AUDIO_FOLDER = "tokki/audio/pronunciation-example";

            var extractedData = await _excelService.ExtractExampleDataAsync(request.File);

            if (extractedData == null || !extractedData.Any())
            {
                return OperationResult<ImportExampleResponse>.Failure(new Error("EXCEL_EMPTY", "Không tìm thấy dữ liệu hợp lệ trong file Excel."));
            }

            var newEntities = new List<Domain.Entities.PronunciationExample>();

            foreach (var item in extractedData)
            {
                try
                {
                    string? audioUrl = null;
                    try
                    {
                        var audioBytes = await _ttsService.SynthesizeKoreanAudioAsync(item.RawScript);
                        audioUrl = await _cloudinaryService.UploadAudioAsync(audioBytes, $"EXAMPLE_{Guid.NewGuid()}", AUDIO_FOLDER);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Lỗi TTS hoặc Cloudinary cho nội dung: {Text}", item.RawScript);
                    }

                    var entity = new Domain.Entities.PronunciationExample
                    {
                        ExampleId = _idGenerator.Generate(10),
                        PronunciationRuleId = item.PronunciationRuleId,
                        TargetScript = item.TargetScript,
                        RawScript = item.RawScript,
                        PhoneticScript = item.PhoneticScript,
                        Meaning = item.Meaning,
                        SortOrder = item.SortOrder,
                        AudioUrl = audioUrl,
                        IsDeleted = false,
                        CreateBy = request.UserId,
                        CreateDate = DateTime.Now
                    };

                    newEntities.Add(entity);

                    response.SuccessList.Add(new ExamplePreviewDTO
                    {
                        TargetScript = item.TargetScript,
                        Meaning = item.Meaning ?? "",
                        Reason = "Thành công"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xử lý dòng dữ liệu: {Target}", item.TargetScript);
                    response.FailureList.Add(new ExamplePreviewDTO
                    {
                        TargetScript = item.TargetScript,
                        Meaning = item.Meaning ?? "",
                        Reason = $"Lỗi: {ex.Message}"
                    });
                }
            }

            if (newEntities.Any())
            {
                try
                {
                    await _exampleRepo.AddRangeAsync(newEntities);
                }
                catch (Exception ex)
                {
                    return OperationResult<ImportExampleResponse>.Failure(AppErrors.DatabaseError);
                }
            }

            var summaryMsg = $"Import hoàn tất. Thành công: {newEntities.Count}, Thất bại: {response.FailureList.Count}";
            return OperationResult<ImportExampleResponse>.Success(response, 200, summaryMsg);
        }
    }
}
