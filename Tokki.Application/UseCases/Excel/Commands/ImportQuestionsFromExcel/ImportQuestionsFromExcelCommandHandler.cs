using MediatR;
using Microsoft.Extensions.Logging;
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

            QuestionBankImportDTO excelData;
            try
            {
                excelData = await _excelService.ExtractQuestionBankDataAsync(request.ExcelFile);
            }
            catch (Exception ex)
            {
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
                    response.Errors.Add(new ImportedQuestionError
                    {
                        ExcelRowIndex = rawP.RowIndex,
                        SheetName = "Passages",
                        ContentSummary = Truncate(rawP.Title),
                        ErrorReason = "MediaType bị trống."
                    });
                    continue;
                }

                if (!Enum.TryParse<PassageMediaType>(rawP.MediaType, true, out var validMediaType) ||
                    !Enum.IsDefined(typeof(PassageMediaType), validMediaType))
                {
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
                    response.Errors.Add(new ImportedQuestionError
                    {
                        ExcelRowIndex = invalidOption.RowIndex,
                        SheetName = "Options",
                        ContentSummary = $"Key: {invalidOption.KeyOption}",
                        ErrorReason = $"IsCorrect '{invalidOption.IsCorrectStr}' sai format."
                    });
                    continue;
                }

                bool hasCorrectAnswer = linkedOptions.Any(o => o.IsCorrectStr == "1");
                if (!hasCorrectAnswer)
                {
                    response.Errors.Add(new ImportedQuestionError
                    {
                        ExcelRowIndex = rawQ.RowIndex,
                        SheetName = "QuestionBanks",
                        ContentSummary = Truncate(rawQ.Content),
                        ErrorReason = "Câu hỏi không có đáp án đúng nào."
                    });
                    continue;
                }

                var realQuestionId = _idGeneratorService.GenerateCustom(10);

                // Link Passage
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
                    MediaUrl = rawQ.MediaUrl,
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
                        ImageUrl = rawOpt.ImageUrl, 
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

            }
            catch (Exception ex)
            {
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