using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Excel.Commands.ImportQuestionsFromExcel
{
    public class ImportQuestionsFromExcelCommandHandler : IRequestHandler<ImportQuestionsFromExcelCommand, OperationResult<ImportQuestionsResponse>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IPassageRepository _passageRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IExcelService _excelService;
        private readonly ILogger<ImportQuestionsFromExcelCommandHandler> _logger;

        public ImportQuestionsFromExcelCommandHandler(
            IQuestionBankRepository questionBankRepository,
            IPassageRepository passageRepository,
            IIdGeneratorService idGeneratorService,
            IExcelService excelService,
            ILogger<ImportQuestionsFromExcelCommandHandler> logger)
        {
            _questionBankRepository = questionBankRepository;
            _passageRepository = passageRepository;
            _idGeneratorService = idGeneratorService;
            _excelService = excelService;
            _logger = logger;
        }

        public async Task<OperationResult<ImportQuestionsResponse>> Handle(ImportQuestionsFromExcelCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("--- BẮT ĐẦU IMPORT EXCEL QUESTION ---");

            QuestionBankImportDTO excelData;
            try
            {
                excelData = await _excelService.ExtractQuestionBankDataAsync(request.ExcelFile);
                _logger.LogInformation($"Đã đọc file Excel. Passages: {excelData.Passages.Count}, Questions: {excelData.Questions.Count}, Options: {excelData.Options.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi đọc file Excel (Service Extract)");
                return OperationResult<ImportQuestionsResponse>.Failure(new Error("Excel.ReadError", ex.Message), 400);
            }

            var response = new ImportQuestionsResponse();
            var passagesToInsert = new List<Passage>();
            var questionsToInsert = new List<QuestionBank>();
            var passageRefMap = new Dictionary<string, Passage>();
            var excelContents = excelData.Questions
                                .Where(q => !string.IsNullOrWhiteSpace(q.Content))
                                .Select(q => q.Content.Trim())
                                .Distinct() 
                                .ToList();

            var existingContents = await _questionBankRepository.GetExistingContentsAsync(excelContents);

            var duplicateSet = new HashSet<string>(existingContents);

            foreach (var rawP in excelData.Passages)
            {
                if (string.IsNullOrWhiteSpace(rawP.RefId)) continue;

                if (string.IsNullOrWhiteSpace(rawP.MediaType))
                {
                    var msg = $"[Passage] Dòng {rawP.RowIndex}: Thiếu MediaType. Title: {Truncate(rawP.Title)}";
                    _logger.LogWarning(msg);

                    response.Errors.Add(new ImportedQuestionError
                    {
                        ExcelRowIndex = rawP.RowIndex,
                        SheetName = "Passages",
                        ContentSummary = Truncate(rawP.Title),
                        ErrorReason = "MediaType bị trống. Bắt buộc phải là: Text, Image, hoặc Audio."
                    });
                    continue;
                }

                if (!Enum.TryParse<PassageMediaType>(rawP.MediaType, true, out var validMediaType) ||
                    !Enum.IsDefined(typeof(PassageMediaType), validMediaType))
                {
                    var msg = $"[Passage] Dòng {rawP.RowIndex}: MediaType '{rawP.MediaType}' không hợp lệ.";
                    _logger.LogWarning(msg);

                    response.Errors.Add(new ImportedQuestionError
                    {
                        ExcelRowIndex = rawP.RowIndex,
                        SheetName = "Passages",
                        ContentSummary = Truncate(rawP.Title),
                        ErrorReason = $"MediaType '{rawP.MediaType}' không hợp lệ."
                    });
                    continue;
                }

                if (!passageRefMap.ContainsKey(rawP.RefId))
                {
                    var realPassageId = _idGeneratorService.GenerateCustom(10);
                    var passage = new Passage
                    {
                        PassageId = realPassageId,
                        Title = rawP.Title ?? "No Title",
                        Content = rawP.Content ?? "",
                        ImageUrl = rawP.ImageUrl,
                        MediaType = validMediaType,
                        Status = PassageStatus.Active,
                        CreatedAt = DateTime.UtcNow
                    };
                    passagesToInsert.Add(passage);
                    passageRefMap.Add(rawP.RefId, passage);
                }
            }

            foreach (var rawQ in excelData.Questions)
            {
                if (string.IsNullOrWhiteSpace(rawQ.Explanation))
                {
                    var msg = $"[Question] Dòng {rawQ.RowIndex}: Thiếu Explanation. Content: {Truncate(rawQ.Content)}";
                    _logger.LogWarning(msg);

                    response.Errors.Add(new ImportedQuestionError
                    {
                        ExcelRowIndex = rawQ.RowIndex,
                        SheetName = "QuestionBanks",
                        ContentSummary = Truncate(rawQ.Content),
                        ErrorReason = "Thiếu giải thích (Explanation)."
                    });
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(rawQ.Content) && duplicateSet.Contains(rawQ.Content.Trim()))
                {
                    var msg = $"[Question] Dòng {rawQ.RowIndex}: Câu hỏi đã tồn tại trong DB. Content: {Truncate(rawQ.Content)}";
                    _logger.LogWarning(msg);

                    response.Errors.Add(new ImportedQuestionError
                    {
                        ExcelRowIndex = rawQ.RowIndex,
                        SheetName = "QuestionBanks",
                        ContentSummary = Truncate(rawQ.Content),
                        ErrorReason = "Duplicate: Câu hỏi này đã tồn tại trong Database."
                    });
                    continue;
                }

                var linkedOptions = excelData.Options.Where(o => o.RefQuestionId == rawQ.RefId).ToList();

                var invalidOption = linkedOptions.FirstOrDefault(o => o.IsCorrectStr != "0" && o.IsCorrectStr != "1");
                if (invalidOption != null)
                {
                    var msg = $"[Option] Dòng {invalidOption.RowIndex}: IsCorrect '{invalidOption.IsCorrectStr}' sai format. Key: {invalidOption.KeyOption}";
                    _logger.LogWarning(msg);

                    response.Errors.Add(new ImportedQuestionError
                    {
                        ExcelRowIndex = invalidOption.RowIndex,
                        SheetName = "Options",
                        ContentSummary = $"Key: {invalidOption.KeyOption} - Content: {Truncate(invalidOption.Content)}",
                        ErrorReason = $"Giá trị IsCorrect '{invalidOption.IsCorrectStr}' không hợp lệ. Chỉ chấp nhận '0' (Sai) hoặc '1' (Đúng)."
                    });
                    continue;
                }

                bool hasCorrectAnswer = linkedOptions.Any(o => o.IsCorrectStr == "1");
                if (!hasCorrectAnswer)
                {
                    var msg = $"[Question] Dòng {rawQ.RowIndex}: Không có đáp án đúng nào.";
                    _logger.LogWarning(msg);

                    response.Errors.Add(new ImportedQuestionError
                    {
                        ExcelRowIndex = rawQ.RowIndex,
                        SheetName = "QuestionBanks",
                        ContentSummary = Truncate(rawQ.Content),
                        ErrorReason = "Câu hỏi không có đáp án đúng nào (Phải có ít nhất một dòng IsCorrect là '1')."
                    });
                    continue;
                }

                var realQuestionId = _idGeneratorService.GenerateCustom(10);

                Passage? linkedPassage = null;
                if (!string.IsNullOrEmpty(rawQ.RefPassageId) && passageRefMap.ContainsKey(rawQ.RefPassageId))
                {
                    linkedPassage = passageRefMap[rawQ.RefPassageId];
                }

                var questionEntity = new QuestionBank
                {
                    QuestionBankId = realQuestionId,
                    PassageId = linkedPassage?.PassageId,
                    QuestionTypeId = request.QuestionTypeId,
                    Content = rawQ.Content,
                    Explanation = rawQ.Explanation,
                    Status = QuestionBankStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    QuestionOptions = new List<QuestionOption>()
                };

                foreach (var rawOpt in linkedOptions)
                {
                    questionEntity.QuestionOptions.Add(new QuestionOption
                    {
                        OptionId = _idGeneratorService.GenerateCustom(10),
                        QuestionBankId = realQuestionId,
                        KeyOption = rawOpt.KeyOption,
                        Content = rawOpt.Content,
                        IsCorrect = rawOpt.IsCorrectStr == "1"
                    });
                }

                questionsToInsert.Add(questionEntity);

                response.SuccessItems.Add(new ImportedQuestionSuccess
                {
                    ExcelRowIndex = rawQ.RowIndex,
                    ExcelRefId = rawQ.RefId,
                    RealId = realQuestionId,
                    Content = rawQ.Content,
                    LinkedPassage = linkedPassage != null ? new ImportedPassageInfo
                    {
                        Title = linkedPassage.Title,
                        RealId = linkedPassage.PassageId
                    } : null,
                    Options = linkedOptions.Select(o => new ImportedOptionInfo
                    {
                        Key = o.KeyOption,
                        Content = o.Content,
                        IsCorrect = o.IsCorrectStr == "1"
                    }).ToList()
                });
            }

            try
            {
                if (passagesToInsert.Any()) await _passageRepository.AddRangeAsync(passagesToInsert);
                if (questionsToInsert.Any()) await _questionBankRepository.AddRangeAsync(questionsToInsert);

                await _questionBankRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation($"--- KẾT THÚC IMPORT --- Thành công: {response.TotalSuccess}, Lỗi: {response.TotalFailed}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu xuống Database!");
                throw; 
            }
            return OperationResult<ImportQuestionsResponse>.Success(
                response,
                200,
                $"Đã xử lý file Excel. Thành công: {response.TotalSuccess}, Lỗi: {response.TotalFailed}"
            );
        }

        private string Truncate(string input)
        {
            return string.IsNullOrEmpty(input) ? "" : (input.Length > 30 ? input.Substring(0, 30) + "..." : input);
        }
    }
}