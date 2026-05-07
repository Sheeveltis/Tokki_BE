using DnsClient.Internal;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Exam.Commands.CreateExam;
using Tokki.Application.UseCases.ExamTemplates.Commands.AddTemplateParts;
using Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Domain.Constants;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Services
{
    public class ExamAssemblyService : IExamAssemblyService
    {
        private readonly IMediator _mediator;
        private readonly TokkiDbContext _context;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ISystemConfigRepository _configRepo;
        private const string AI_SYSTEM_ID = "AI_EXAM_SYSTEM";

        private const int DEFAULT_QUESTIONS_PER_PART_MCQ = 10;
        private const int DEFAULT_QUESTIONS_PER_PART_WRITING = 2;
        private const int DEFAULT_MINUTES_PER_MCQ_QUESTION = 2;
        private const int DEFAULT_MINUTES_PER_WRITING_QUESTION = 20;

        private readonly ILogger<ExamAssemblyService> _logger;
        public ExamAssemblyService(
            IMediator mediator,
            TokkiDbContext context,
            IIdGeneratorService idGenerator,
            ISystemConfigRepository configRepo,
            ILogger<ExamAssemblyService> logger)
        {
            _mediator = mediator;
            _context = context;
            _idGenerator = idGenerator;
            _configRepo = configRepo;
            _logger = logger;
        }
        private async Task<int> GetIntConfigAsync(string key, int fallback)
        {
            try
            {
                var cfg = await _configRepo.GetByKeyAsync(key);
                if (cfg is { IsActive: true, Value: not null }
                    && int.TryParse(cfg.Value, out int parsed))
                    return parsed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không đọc được config '{Key}', dùng fallback={Fallback}.", key, fallback);
            }
            return fallback;
        }

        public async Task<OperationResult<string>> GenerateWeeklyExamFromScopeAsync(
            string userId,
            int weekIndex,
            List<string> weeklyQuestionTypeIds,
            ExamType examType,
            CancellationToken cancellationToken = default)
        {
            int questionsPerPartMcq = await GetIntConfigAsync(PromptConfigKeys.QuestionsPerPartMcq, DEFAULT_QUESTIONS_PER_PART_MCQ);
            int questionsPerPartWriting = await GetIntConfigAsync(PromptConfigKeys.QuestionsPerPartWriting, DEFAULT_QUESTIONS_PER_PART_WRITING);
            int minutesPerMcqQuestion = await GetIntConfigAsync(PromptConfigKeys.MinutesPerMcqQuestion, DEFAULT_MINUTES_PER_MCQ_QUESTION);
            int minutesPerWritingQuestion = await GetIntConfigAsync(PromptConfigKeys.MinutesPerWritingQuestion, DEFAULT_MINUTES_PER_WRITING_QUESTION);

            var targetTypes = weeklyQuestionTypeIds.Distinct().OrderBy(x => x).ToList();
            if (!targetTypes.Any())
                return OperationResult<string>.Failure("Không có dạng bài nào để tạo đề.", 400);

            var (isBankValid, insufficientTypes) = await ValidateQuestionAvailabilityAsync(targetTypes, cancellationToken);
            if (!isBankValid)
            {
                _logger.LogWarning(
                    "GenerateWeeklyExam tuần {Week}: Question bank không đủ câu cho {Count} dạng: [{Types}]. " +
                    "Tiếp tục tạo exam nhưng có thể thiếu câu.",
                    weekIndex, insufficientTypes.Count, string.Join(", ", insufficientTypes));
            }

            var questionTypes = await _context.QuestionTypes
                .Where(qt => targetTypes.Contains(qt.QuestionTypeId))
                .ToListAsync(cancellationToken);

            var qtMap = questionTypes.ToDictionary(qt => qt.QuestionTypeId);
            string structureHash = string.Join("|", targetTypes);
            string templateIdToUse;

            var existingStructure = await _context.ExamTemplateStructures
                .AsNoTracking()
                .Include(x => x.ExamTemplate)
                .FirstOrDefaultAsync(x => x.StructureHash == structureHash, cancellationToken);

            bool canReuse = existingStructure != null
                && existingStructure.ExamTemplate != null
                && existingStructure.ExamTemplate.Type == examType
                && existingStructure.ExamTemplate.Status == ExamTemplateStatus.Published;

            if (canReuse)
            {
                templateIdToUse = existingStructure!.ExamTemplateId;
                _logger.LogInformation(
                    "Tái sử dụng template {TemplateId} cho scope [{Scope}]",
                    templateIdToUse, structureHash);
            }
            else
            {
                if (existingStructure != null)
                {
                    _logger.LogWarning(
                        "Template {TemplateId} không hợp lệ (ExamType mismatch hoặc chưa Published). Tạo lại template mới.",
                        existingStructure.ExamTemplateId);
                    _context.ExamTemplateStructures.Remove(existingStructure);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                string examTypeLabel = examType == ExamType.TopikI ? "TOPIK I" : "TOPIK II";
                var codeParts = targetTypes
                    .Select(id => qtMap.TryGetValue(id, out var qt)
                        ? (qt.Code ?? qt.Name)
                        : id);
                string templateName = $"[WEEKLY] {examTypeLabel} | {string.Join(" · ", codeParts)}";

                var createTemplateCmd = new CreateExamTemplateCommand
                {
                    Name = templateName,
                    Description = "Auto-generated by Adaptive Learning System",
                    Type = examType,
                    CreatedBy = AI_SYSTEM_ID
                };

                var templateResult = await _mediator.Send(createTemplateCmd, cancellationToken);
                if (!templateResult.IsSuccess)
                    return OperationResult<string>.Failure($"Lỗi tạo Template: {templateResult.Message}");

                string newTemplateId = templateResult.Data;

                var partsDto = new List<CreateTemplatePartDto>();
                int currentQ = 1;

                var orderedTypes = targetTypes
                    .OrderBy(id => qtMap.TryGetValue(id, out var qt) ? (int)qt.Skill : 99);

                foreach (var typeId in orderedTypes)
                {
                    qtMap.TryGetValue(typeId, out var qType);
                    QuestionSkill skill = qType?.Skill ?? QuestionSkill.Reading;

                    int questionsForType = skill == QuestionSkill.Writing
                        ? questionsPerPartWriting
                        : questionsPerPartMcq;

                    int markPerQuestion = 2;
                    if (skill == QuestionSkill.Writing)
                    {
                        string code = qType?.Code ?? "";
                        if (code.Contains("53"))
                            markPerQuestion = 30;
                        else if (code.Contains("54"))
                            markPerQuestion = 50;
                        else
                            markPerQuestion = 10;
                    }

                    partsDto.Add(new CreateTemplatePartDto
                    {
                        PartTitle = qType?.Name ?? typeId,
                        QuestionTypeId = typeId,
                        Skill = skill,
                        QuestionFrom = currentQ,
                        QuestionTo = currentQ + questionsForType - 1,
                        Mark = markPerQuestion,
                        Instruction = skill == QuestionSkill.Writing
                            ? "Hãy viết bài theo yêu cầu."
                            : "Choose the best answer."
                    });
                    currentQ += questionsForType;
                }

                var addPartsResult = await _mediator.Send(
                    new AddTemplatePartsCommand { ExamTemplateId = newTemplateId, Parts = partsDto },
                    cancellationToken);

                if (!addPartsResult.IsSuccess)
                    return OperationResult<string>.Failure($"Lỗi tạo parts: {addPartsResult.Message}");

                var tpl = await _context.ExamTemplates.FindAsync(newTemplateId);
                if (tpl != null) tpl.Status = ExamTemplateStatus.Published;

                _context.ExamTemplateStructures.Add(new ExamTemplateStructure
                {
                    Id = _idGenerator.GenerateCustom(15),
                    StructureHash = structureHash,
                    ExamTemplateId = newTemplateId,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync(cancellationToken);
                templateIdToUse = newTemplateId;
                _logger.LogInformation(
                    "Tạo template mới {TemplateId} ({Name}) cho scope [{Scope}]",
                    newTemplateId, templateName, structureHash);
            }

            var skillDurations = new Dictionary<string, int>();

            foreach (var typeId in targetTypes)
            {
                if (!qtMap.TryGetValue(typeId, out var qt)) continue;

                string skillKey = qt.Skill.ToString();

                int questionsForType = qt.Skill == QuestionSkill.Writing
                    ? questionsPerPartWriting
                    : questionsPerPartMcq;

                int minutesForType = qt.Skill == QuestionSkill.Writing
                    ? questionsForType * minutesPerWritingQuestion
                    : questionsForType * minutesPerMcqQuestion;

                if (!skillDurations.ContainsKey(skillKey))
                    skillDurations[skillKey] = 0;

                skillDurations[skillKey] += minutesForType;
            }

            string examTitle = $"Weekly Exam W{weekIndex} - {userId[..Math.Min(6, userId.Length)]} - {DateTime.UtcNow:ddMMHHmmss}";

            var createExamCmd = new CreateExamCommand
            {
                Title = examTitle,
                ExamTemplateId = templateIdToUse,
                CreatedBy = AI_SYSTEM_ID,
                SkillDurations = skillDurations
            };

            var examResult = await _mediator.Send(createExamCmd, cancellationToken);

            if (examResult.IsSuccess)
            {
                var exam = await _context.Exams.FindAsync(examResult.Data);
                if (exam != null)
                {
                    exam.Status = ExamStatus.Published;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                _logger.LogInformation(
                    "Tạo weekly exam {ExamId} tuần {Week} thành công (Published).", examResult.Data, weekIndex);
            }
            else
            {
                _logger.LogError(
                    "Tạo weekly exam tuần {Week} thất bại: {Msg}", weekIndex, examResult.Message);
            }

            return examResult;
        }
        [Obsolete("Deprecated — dùng GenerateWeeklyExamFromScopeAsync thay thế. Method này sẽ bị xóa trong phiên bản tiếp theo.")]
        public async Task<OperationResult<string>> GenerateWeeklyExamAsync(
            string templateId,
            string userId,
            int weekIndex,
            List<string> weakQuestionTypeIds,
            DifficultyLevel targetLevel,
            CancellationToken cancellationToken = default)
        {
            _logger.LogError(
                "GenerateWeeklyExamAsync bị gọi — đây là method deprecated, không có tác dụng. " +
                "Caller: templateId={TemplateId}, userId={UserId}, weekIndex={WeekIndex}. " +
                "Hãy dùng GenerateWeeklyExamFromScopeAsync.",
                templateId, userId, weekIndex);
            return OperationResult<string>.Failure("Deprecated method — dùng GenerateWeeklyExamFromScopeAsync.");
        }
        public async Task<(bool IsValid, List<string> InsufficientTypes)> ValidateQuestionAvailabilityAsync(
            List<string> questionTypeIds,
            CancellationToken cancellationToken = default)
        {
            int questionsPerPartMcq = await GetIntConfigAsync(PromptConfigKeys.QuestionsPerPartMcq, DEFAULT_QUESTIONS_PER_PART_MCQ);
            int questionsPerPartWriting = await GetIntConfigAsync(PromptConfigKeys.QuestionsPerPartWriting, DEFAULT_QUESTIONS_PER_PART_WRITING);

            var insufficientTypes = new List<string>();

            foreach (var typeId in questionTypeIds)
            {
                var qType = await _context.QuestionTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.QuestionTypeId == typeId, cancellationToken);

                if (qType == null) continue;

                int required = qType.Skill == QuestionSkill.Writing
                    ? questionsPerPartWriting
                    : questionsPerPartMcq;

                int available = await _context.QuestionBank
                    .CountAsync(q => q.QuestionTypeId == typeId
                                  && q.Status == QuestionBankStatus.Active,
                        cancellationToken);

                if (available < required)
                {
                    insufficientTypes.Add(typeId);
                    _logger.LogWarning(
                        "QuestionType {TypeId} (Skill={Skill}, Difficulty={Diff}): cần {Required} câu, chỉ có {Available} câu active.",
                        typeId, qType.Skill, qType.Difficulty, required, available);
                }
            }

            return (!insufficientTypes.Any(), insufficientTypes);
        }

        private static int ParseQuestionCountFromCode(string? code)
        {
            if (string.IsNullOrEmpty(code)) return 1;

            var parts = code.Split('_');
            if (parts.Length >= 2)
            {
                var last = parts[^1];
                var secondLast = parts[^2];

                if (secondLast.StartsWith("Q", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(secondLast[1..], out int from)
                    && int.TryParse(last, out int to)
                    && to >= from)
                {
                    return to - from + 1;
                }

                if (last.StartsWith("Q", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(last[1..], out _))
                {
                    return 1;
                }
            }

            return 1;
        }

        public async Task<OperationResult<string>> GenerateTopikStyleExamAsync(
            string userId,
            int weekIndex,
            List<string> weaknessTypeIds,
            ExamType examType,
            CancellationToken cancellationToken = default)
        {
            int minutesPerMcqQuestion = await GetIntConfigAsync(PromptConfigKeys.MinutesPerMcqQuestion, DEFAULT_MINUTES_PER_MCQ_QUESTION);
            int minutesPerWritingQuestion = await GetIntConfigAsync(PromptConfigKeys.MinutesPerWritingQuestion, DEFAULT_MINUTES_PER_WRITING_QUESTION);

            var targetTypes = weaknessTypeIds.Distinct().OrderBy(x => x).ToList();
            if (!targetTypes.Any())
                return OperationResult<string>.Failure("Không có dạng bài nào để tạo đề tổng hợp.", 400);

            var (isBankValid, insufficientTypes) = await ValidateQuestionAvailabilityAsync(targetTypes, cancellationToken);
            if (!isBankValid)
            {
                _logger.LogWarning(
                    "GenerateTopikStyleExam tuần {Week}: Question bank không đủ câu cho {Count} dạng: [{Types}]. " +
                    "Tiếp tục tạo exam nhưng có thể thiếu câu.",
                    weekIndex, insufficientTypes.Count, string.Join(", ", insufficientTypes));
            }

            var questionTypes = await _context.QuestionTypes
                .Where(qt => targetTypes.Contains(qt.QuestionTypeId))
                .ToListAsync(cancellationToken);

            var qtMap = questionTypes.ToDictionary(qt => qt.QuestionTypeId);

            string structureHash = "TOPIK_STYLE|" + string.Join("|", targetTypes);
            string templateIdToUse;

            var existingStructure = await _context.ExamTemplateStructures
                .AsNoTracking()
                .Include(x => x.ExamTemplate)
                .FirstOrDefaultAsync(x => x.StructureHash == structureHash, cancellationToken);

            bool canReuse = existingStructure != null
                && existingStructure.ExamTemplate != null
                && existingStructure.ExamTemplate.Type == examType
                && existingStructure.ExamTemplate.Status == ExamTemplateStatus.Published;

            if (canReuse)
            {
                templateIdToUse = existingStructure!.ExamTemplateId;
                _logger.LogInformation(
                    "Tái sử dụng TOPIK-style template {TemplateId} cho expansion exam", templateIdToUse);
            }
            else
            {
                if (existingStructure != null)
                {
                    _context.ExamTemplateStructures.Remove(existingStructure);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                string examTypeLabel = examType == ExamType.TopikI ? "TOPIK I" : "TOPIK II";
                string templateName = $"[EXPANSION] {examTypeLabel} | Tổng hợp {targetTypes.Count} dạng";

                var createTemplateCmd = new CreateExamTemplateCommand
                {
                    Name = templateName,
                    Description = "Auto-generated Expansion Phase exam — TOPIK structure",
                    Type = examType,
                    CreatedBy = AI_SYSTEM_ID
                };

                var templateResult = await _mediator.Send(createTemplateCmd, cancellationToken);
                if (!templateResult.IsSuccess)
                    return OperationResult<string>.Failure($"Lỗi tạo template tổng hợp: {templateResult.Message}");

                string newTemplateId = templateResult.Data;
                var partsDto = new List<CreateTemplatePartDto>();
                int currentQ = 1;

                var orderedTypes = targetTypes
                    .OrderBy(id => qtMap.TryGetValue(id, out var qt) ? (int)qt.Skill : 99)
                    .ThenBy(id => qtMap.TryGetValue(id, out var qt) ? qt.OrderIndex : 999);

                foreach (var typeId in orderedTypes)
                {
                    qtMap.TryGetValue(typeId, out var qType);
                    QuestionSkill skill = qType?.Skill ?? QuestionSkill.Reading;

                    int questionsForType = ParseQuestionCountFromCode(qType?.Code);

                    int markPerQuestion = 2;
                    if (skill == QuestionSkill.Writing)
                    {
                        string code = qType?.Code ?? "";
                        if (code.Contains("53")) markPerQuestion = 30;
                        else if (code.Contains("54")) markPerQuestion = 50;
                        else markPerQuestion = 10;
                    }

                    partsDto.Add(new CreateTemplatePartDto
                    {
                        PartTitle = qType?.Name ?? typeId,
                        QuestionTypeId = typeId,
                        Skill = skill,
                        QuestionFrom = currentQ,
                        QuestionTo = currentQ + questionsForType - 1,
                        Mark = markPerQuestion,
                        Instruction = skill == QuestionSkill.Writing
                            ? "Hãy viết bài theo yêu cầu."
                            : "Choose the best answer."
                    });
                    currentQ += questionsForType;
                }

                var addPartsResult = await _mediator.Send(
                    new AddTemplatePartsCommand { ExamTemplateId = newTemplateId, Parts = partsDto },
                    cancellationToken);

                if (!addPartsResult.IsSuccess)
                    return OperationResult<string>.Failure($"Lỗi tạo parts tổng hợp: {addPartsResult.Message}");

                var tpl = await _context.ExamTemplates.FindAsync(newTemplateId);
                if (tpl != null) tpl.Status = ExamTemplateStatus.Published;

                _context.ExamTemplateStructures.Add(new ExamTemplateStructure
                {
                    Id = _idGenerator.GenerateCustom(15),
                    StructureHash = structureHash,
                    ExamTemplateId = newTemplateId,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync(cancellationToken);
                templateIdToUse = newTemplateId;
                _logger.LogInformation("Tạo TOPIK-style template {TemplateId} cho expansion exam", newTemplateId);
            }

            var skillDurations = new Dictionary<string, int>();
            foreach (var typeId in targetTypes)
            {
                if (!qtMap.TryGetValue(typeId, out var qt)) continue;
                string skillKey = qt.Skill.ToString();
                int questionsForType = ParseQuestionCountFromCode(qt.Code);
                int minutesForType = qt.Skill == QuestionSkill.Writing
                    ? questionsForType * minutesPerWritingQuestion
                    : questionsForType * minutesPerMcqQuestion;

                if (!skillDurations.ContainsKey(skillKey))
                    skillDurations[skillKey] = 0;
                skillDurations[skillKey] += minutesForType;
            }

            string examTitle = $"[Expansion] TOPIK Exam W{weekIndex} - {userId[..Math.Min(6, userId.Length)]} - {DateTime.UtcNow:ddMMHHmmss}";

            var createExamCmd = new CreateExamCommand
            {
                Title = examTitle,
                ExamTemplateId = templateIdToUse,
                CreatedBy = AI_SYSTEM_ID,
                SkillDurations = skillDurations
            };

            var examResult = await _mediator.Send(createExamCmd, cancellationToken);

            if (examResult.IsSuccess)
            {
                var exam = await _context.Exams.FindAsync(examResult.Data);
                if (exam != null)
                {
                    exam.Status = ExamStatus.Published;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                _logger.LogInformation("Tạo expansion exam {ExamId} tuần {Week} thành công.", examResult.Data, weekIndex);
            }

            return examResult;
        }
    }
}