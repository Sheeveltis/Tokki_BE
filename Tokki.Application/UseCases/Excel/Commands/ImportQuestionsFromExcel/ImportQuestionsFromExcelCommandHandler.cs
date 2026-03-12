using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using System.Text;
using System.Text.RegularExpressions;

namespace Tokki.Application.UseCases.Excel.Commands.ImportQuestionsFromExcel
{
    public class ImportQuestionsFromExcelCommandHandler : IRequestHandler<ImportQuestionsFromExcelCommand, OperationResult<ImportQuestionsResponse>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IPassageRepository _passageRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IExcelService _excelService;
        private readonly ILogger<ImportQuestionsFromExcelCommandHandler> _logger;

        private static readonly Regex _whitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

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
            var cleanQuestionTypeId = request.QuestionTypeId?.Trim();
            if (string.IsNullOrEmpty(cleanQuestionTypeId))
            {
                return OperationResult<ImportQuestionsResponse>.Failure(new Error("Validation", "QuestionTypeId không được để trống."), 400);
            }

            QuestionBankImportDTO excelData;
            try
            {
                excelData = await _excelService.ExtractQuestionBankDataAsync(request.ExcelFile);
            }
            catch (Exception ex)
            {
                return OperationResult<ImportQuestionsResponse>.Failure(new Error("Excel.ReadError", ex.Message), 400);
            }

            foreach (var q in excelData.Questions)
            {
                q.Content = StandardizeText(q.Content);
                q.Explanation = StandardizeText(q.Explanation);
                q.MediaUrl = q.MediaUrl?.Trim();
            }
            foreach (var opt in excelData.Options)
            {
                opt.Content = StandardizeText(opt.Content);
            }

            var response = new ImportQuestionsResponse();
            var passagesToInsert = new List<Passage>();
            var questionsToInsert = new List<QuestionBank>();
            var passageRefMap = new Dictionary<string, Passage>();

            var excelContents = excelData.Questions
                                    .Where(q => !string.IsNullOrWhiteSpace(q.Content))
                                    .Select(q => q.Content)
                                    .Distinct()
                                    .ToList();

            var existingQuestionDTOs = await _questionBankRepository.GetQuestionsByTypeAsync(cleanQuestionTypeId);

            var existingSignatures = new HashSet<string>();
            foreach (var dto in existingQuestionDTOs)
            {
                existingSignatures.Add(GenerateQuestionSignature(dto.Content, dto.MediaUrl, dto.OptionContents));
            }

            foreach (var rawP in excelData.Passages)
            {
                if (string.IsNullOrWhiteSpace(rawP.RefId)) continue;
                if (string.IsNullOrWhiteSpace(rawP.MediaType)) continue;

                if (!Enum.TryParse<PassageMediaType>(rawP.MediaType, true, out var validMediaType)) continue;

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
                if (string.IsNullOrWhiteSpace(rawQ.Explanation)) continue;

                var linkedOptionsRaw = excelData.Options.Where(o => o.RefQuestionId == rawQ.RefId).ToList();

                var invalidOption = linkedOptionsRaw.FirstOrDefault(o => o.IsCorrectStr != "0" && o.IsCorrectStr != "1");
                if (invalidOption != null)
                {
                    response.Errors.Add(new ImportedQuestionError { ExcelRowIndex = invalidOption.RowIndex, SheetName = "Options", ContentSummary = $"Key: {invalidOption.KeyOption}", ErrorReason = $"IsCorrect '{invalidOption.IsCorrectStr}' sai format." });
                    continue;
                }

                bool hasOptions = linkedOptionsRaw.Any();
                bool hasCorrectAnswer = linkedOptionsRaw.Any(o => o.IsCorrectStr == "1");

                if (hasOptions && !hasCorrectAnswer)
                {
                    response.Errors.Add(new ImportedQuestionError
                    {
                        ExcelRowIndex = rawQ.RowIndex,
                        SheetName = "QuestionBanks",
                        ContentSummary = Truncate(rawQ.Content),
                        ErrorReason = "Câu hỏi trắc nghiệm nhưng không có đáp án đúng nào."
                    });
                    continue;
                }
                var currentOptionsForSig = linkedOptionsRaw.Select(o => o.Content).ToList();
                var currentSignature = GenerateQuestionSignature(rawQ.Content, rawQ.MediaUrl, currentOptionsForSig);
                if (existingSignatures.Contains(currentSignature))
                {
                    response.Errors.Add(new ImportedQuestionError
                    {
                        ExcelRowIndex = rawQ.RowIndex,
                        SheetName = "QuestionBanks",
                        ContentSummary = Truncate(rawQ.Content),
                        ErrorReason = "Duplicate: Câu hỏi này đã tồn tại trong DB."
                    });
                    continue;
                }

                if (questionsToInsert.Any(q => {
                    var qOptionContents = q.QuestionOptions.Select(o => o.Content).ToList();
                    return GenerateQuestionSignature(q.Content, q.MediaUrl, qOptionContents) == currentSignature;
                }))
                {
                    response.Errors.Add(new ImportedQuestionError
                    {
                        ExcelRowIndex = rawQ.RowIndex,
                        SheetName = "QuestionBanks",
                        ContentSummary = Truncate(rawQ.Content),
                        ErrorReason = "Duplicate: Trùng lặp ngay trong file Excel."
                    });
                    continue;
                }

                // --- TẠO ENTITY ---
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
                    QuestionTypeId = cleanQuestionTypeId,
                    Content = rawQ.Content,
                    Explanation = rawQ.Explanation,
                    MediaUrl = rawQ.MediaUrl,
                    Status = QuestionBankStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    QuestionOptions = new List<QuestionOption>()
                };

                foreach (var rawOpt in linkedOptionsRaw)
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
                existingSignatures.Add(currentSignature);

                response.SuccessItems.Add(new ImportedQuestionSuccess
                {
                    ExcelRowIndex = rawQ.RowIndex,
                    ExcelRefId = rawQ.RefId,
                    RealId = realQuestionId,
                    Content = rawQ.Content,
                    LinkedPassage = linkedPassage != null ? new ImportedPassageInfo { Title = linkedPassage.Title, RealId = linkedPassage.PassageId } : null,
                    Options = linkedOptionsRaw.Select(o => new ImportedOptionInfo { Key = o.KeyOption, Content = o.Content, IsCorrect = o.IsCorrectStr == "1" }).ToList()
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
                response, 200, $"Hoàn tất. Thành công: {response.TotalSuccess}, Lỗi: {response.TotalFailed}");
        }

        private string GenerateQuestionSignature(string? content, string? mediaUrl, IEnumerable<string> optionContents)
        {
            var contentStr = StandardizeText(content).ToLower();
            var mediaStr = (mediaUrl ?? "").Trim().ToLower();

            if (string.IsNullOrEmpty(contentStr) && string.IsNullOrEmpty(mediaStr) && (!optionContents.Any())) return "";

            var sb = new StringBuilder();
            sb.Append(contentStr);
            sb.Append("|");
            sb.Append(mediaStr);
            sb.Append("|");

            var sortedOptions = optionContents
                                .Where(o => !string.IsNullOrWhiteSpace(o))
                                .Select(o => StandardizeText(o).ToLower())
                                .OrderBy(c => c)
                                .ToList();

            foreach (var opt in sortedOptions)
            {
                sb.Append(opt);
                sb.Append("|");
            }

            return sb.ToString();
        }

        private string StandardizeText(string? input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return _whitespaceRegex.Replace(input, " ").Trim();
        }

        private string Truncate(string input) => string.IsNullOrEmpty(input) ? "" : (input.Length > 30 ? input.Substring(0, 30) + "..." : input);
    }
}