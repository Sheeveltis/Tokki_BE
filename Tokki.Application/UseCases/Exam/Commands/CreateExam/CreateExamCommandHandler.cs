using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Commands.CreateExam
{
    public class CreateExamCommandHandler : IRequestHandler<CreateExamCommand, OperationResult<string>>
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<CreateExamCommandHandler> _logger;

        public CreateExamCommandHandler(
            IExamRepository examRepository,
            IExamTemplateRepository examTemplateRepository,
            ITemplatePartRepository templatePartRepository,
            IQuestionBankRepository questionBankRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<CreateExamCommandHandler> logger)
        {
            _examRepository = examRepository;
            _examTemplateRepository = examTemplateRepository;
            _templatePartRepository = templatePartRepository;
            _questionBankRepository = questionBankRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(CreateExamCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bắt đầu tạo Exam từ TemplateId: {ExamTemplateId}, Title: {Title}",
                request.ExamTemplateId, request.Title);

            try
            {
                // 1. Kiểm tra trùng tên
                if (await _examRepository.IsTitleExistsAsync(request.Title, null, cancellationToken))
                    return OperationResult<string>.Failure($"Tên đề thi '{request.Title}' đã tồn tại.", 400);

                // 2. Lấy Template và các Part
                var template = await _examTemplateRepository.GetByIdAsync(request.ExamTemplateId);
                if (template == null) return OperationResult<string>.Failure(AppErrors.ExamTemplateNotFound, 404);
                if (template.Status != ExamTemplateStatus.Published)
                    return OperationResult<string>.Failure(AppErrors.ExamTemplateInactive, 400);

                var parts = (await _templatePartRepository.GetByExamTemplateIdAsync(template.ExamTemplateId, cancellationToken))
                            ?.OrderBy(p => p.QuestionFrom).ToList();
                if (parts == null || !parts.Any())
                    return OperationResult<string>.Failure(AppErrors.ExamTemplateEmptyParts, 400);

                // 3. Xử lý thời gian
                var (durationResult, skillDurations, totalDuration) = ValidateAndCalculateDurations(parts, request.SkillDurations);
                if (!durationResult.IsSuccess) return OperationResult<string>.Failure(durationResult.Message, durationResult.StatusCode);

                // 4. Lấy danh sách câu hỏi cần tránh (Anti-duplication - 5 đề gần nhất)
                var avoidIds = await _examRepository.GetRecentQuestionIdsAsync(5, cancellationToken);

                // 5. Bốc câu hỏi cho từng Part (Pairing Logic)
                var selectedQuestions = await PickQuestionsForPartsAsync(parts, avoidIds, cancellationToken);
                if (!selectedQuestions.IsSuccess) return OperationResult<string>.Failure(selectedQuestions.Message, selectedQuestions.StatusCode);

                // 6. Tạo record Exam
                var examId = _idGeneratorService.GenerateCustom(10);
                var exam = new Domain.Entities.Exam
                {
                    ExamId = examId,
                    ExamTemplateId = template.ExamTemplateId,
                    Title = request.Title,
                    Type = template.Type,
                    Status = ExamStatus.Draft,
                    Duration = totalDuration,
                    SkillDurations = System.Text.Json.JsonSerializer.Serialize(skillDurations),
                    CreatedBy = request.CreatedBy,
                    ExamQuestions = selectedQuestions.Data.Select(sq => new ExamQuestion
                    {
                        ExamQuestionId = _idGeneratorService.GenerateCustom(10),
                        ExamId = examId,
                        QuestionBankId = sq.QuestionId,
                        QuestionNo = sq.QuestionNo,
                        Score = sq.Mark
                    }).ToList()
                };

                await _examRepository.AddAsync(exam);
                await _examRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(exam.ExamId, 201, OperationMessages.CreateSuccess("đề thi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo Exam");
                return OperationResult<string>.Failure("Lỗi hệ thống khi tạo đề thi.", 500);
            }
        }

        private (OperationResult<bool> Result, Dictionary<string, int> Durations, int Total) ValidateAndCalculateDurations(
            List<TemplatePart> parts, Dictionary<string, int> input)
        {
            var skillsInTemplate = parts.Select(p => p.Skill.ToString()).Distinct().ToList();
            var finalDurations = new Dictionary<string, int>();
            var inputDurations = new Dictionary<string, int>(input, StringComparer.OrdinalIgnoreCase);

            foreach (var skillName in skillsInTemplate)
            {
                if (!inputDurations.TryGetValue(skillName, out int time) || time <= 0)
                    return (OperationResult<bool>.Failure($"Vui lòng nhập thời gian cho phần '{skillName}'.", 400), null, 0);
                finalDurations[skillName] = time;
            }

            return (OperationResult<bool>.Success(true), finalDurations, finalDurations.Values.Sum());
        }

        private async Task<OperationResult<List<SelectedQuestion>>> PickQuestionsForPartsAsync(
            List<TemplatePart> parts, List<string> avoidIds, CancellationToken ct)
        {
            var allSelectedIds = new List<string>();
            var result = new List<SelectedQuestion>();

            foreach (var part in parts)
            {
                int quantityNeeded = part.QuestionTo - part.QuestionFrom + 1;
                if (quantityNeeded <= 0) continue;

                // Lấy tất cả câu hỏi khả dụng cho Type này
                var candidates = await _questionBankRepository.GetByQuestionTypeIdAsync(part.QuestionTypeId, QuestionBankStatus.Active, ct);
                
                // Lọc bỏ những câu đã chọn trong đề này
                var filtered = candidates.Where(q => !allSelectedIds.Contains(q.QuestionBankId)).ToList();

                // Phân loại: Ưu tiên câu hỏi KHÔNG nằm trong danh sách tránh (Diversity)
                var freshCandidates = filtered.Where(q => !avoidIds.Contains(q.QuestionBankId)).ToList();
                var usedCandidates = filtered.Where(q => avoidIds.Contains(q.QuestionBankId)).ToList();

                var pickedForPart = new List<QuestionBank>();
                
                // Thuật toán: Thử bốc từ fresh trước, nếu không đủ thì bốc từ used
                pickedForPart = SelectWithPairing(freshCandidates, quantityNeeded, part.Skill);
                
                if (pickedForPart.Count < quantityNeeded)
                {
                    var stillNeed = quantityNeeded - pickedForPart.Count;
                    var extra = SelectWithPairing(usedCandidates, stillNeed, part.Skill);
                    pickedForPart.AddRange(extra);
                }

                if (pickedForPart.Count < quantityNeeded)
                    return OperationResult<List<SelectedQuestion>>.Failure(AppErrors.ExamNotEnoughQuestions(part.QuestionTypeId, quantityNeeded, pickedForPart.Count), 400);

                int currentNo = part.QuestionFrom;
                foreach (var q in pickedForPart)
                {
                    allSelectedIds.Add(q.QuestionBankId);
                    result.Add(new SelectedQuestion { QuestionId = q.QuestionBankId, QuestionNo = currentNo++, Mark = part.Mark });
                }
            }

            return OperationResult<List<SelectedQuestion>>.Success(result);
        }

        private List<QuestionBank> SelectWithPairing(List<QuestionBank> pool, int quantity, QuestionSkill skill)
        {
            var selected = new List<QuestionBank>();
            if (quantity <= 0 || !pool.Any()) return selected;

            // Xác định GroupKey dựa trên Skill
            // - Reading: Ưu tiên PassageId
            // - Listening: Ưu tiên MediaUrl (Audio)
            // Nếu không có, gán GUID để coi như đơn lẻ
            var groups = pool.GroupBy(q => 
            {
                if (skill == QuestionSkill.Listening && !string.IsNullOrEmpty(q.MediaUrl))
                    return q.MediaUrl;
                
                if (!string.IsNullOrEmpty(q.PassageId))
                    return q.PassageId;
                
                return Guid.NewGuid().ToString();
            })
            .OrderBy(g => Guid.NewGuid()) // Ngẫu nhiên hóa các nhóm
            .ToList();

            // Ưu tiên 1: Tìm nhóm có số lượng đúng bằng số lượng cần (vd: cần 1 cặp 2 câu, tìm nhóm có đúng 2 câu)
            var exactGroup = groups.FirstOrDefault(g => g.Count() == quantity);
            if (exactGroup != null) return exactGroup.ToList();

            // Ưu tiên 2: Bốc các nhóm sao cho tổng gần bằng hoặc bằng số lượng cần
            foreach (var group in groups)
            {
                if (selected.Count + group.Count() <= quantity)
                {
                    selected.AddRange(group);
                    if (selected.Count == quantity) break;
                }
            }

            // Ưu tiên 3: Nếu vẫn chưa đủ (do các nhóm còn lại quá lớn), bốc lẻ từ các nhóm lớn
            if (selected.Count < quantity)
            {
                var remainingNeeded = quantity - selected.Count;
                var leftOverQuestions = groups.Where(g => !selected.Any(s => s.QuestionBankId == g.First().QuestionBankId))
                                              .SelectMany(g => g)
                                              .Take(remainingNeeded)
                                              .ToList();
                selected.AddRange(leftOverQuestions);
            }

            return selected;
        }

        private class SelectedQuestion
        {
            public string QuestionId { get; set; } = "";
            public int QuestionNo { get; set; }
            public int Mark { get; set; }
        }
    }
}