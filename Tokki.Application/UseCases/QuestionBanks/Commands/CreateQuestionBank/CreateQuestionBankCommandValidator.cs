using FluentValidation;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank
{
    public class CreateQuestionBankCommandValidator : AbstractValidator<CreateQuestionBankCommand>
    {
        private readonly IQuestionTypeRepository _questionTypeRepository;

        public CreateQuestionBankCommandValidator(IQuestionTypeRepository questionTypeRepository)
        {
            _questionTypeRepository = questionTypeRepository;

            RuleFor(x => x.QuestionTypeId)
                .NotEmpty()
                .WithName("Loại câu hỏi");

            // NOTE: Bỏ RuleFor(x => x.Content).NotEmpty() để đồng bộ với Update
            // Validate theo QuestionType.Skill trong DB
            RuleFor(x => x).CustomAsync(ValidateByQuestionTypeSkillAsync);
        }

        private async Task ValidateByQuestionTypeSkillAsync(
            CreateQuestionBankCommand model,
            ValidationContext<CreateQuestionBankCommand> context,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(model.QuestionTypeId))
                return; // Rule NotEmpty sẽ bắt

            var questionTypeId = model.QuestionTypeId.Trim();

            var questionType = await _questionTypeRepository.GetByIdAsync(questionTypeId, cancellationToken);
            if (questionType == null)
            {
                context.AddFailure(nameof(CreateQuestionBankCommand.QuestionTypeId), AppErrors.QuestionTypeNotFound.Description);
                return;
            }

            if (!questionType.IsActive)
            {
                context.AddFailure(nameof(CreateQuestionBankCommand.QuestionTypeId), "Loại câu hỏi đang bị vô hiệu hóa.");
                return;
            }

            var skill = questionType.Skill;

            // ===== Đồng bộ rule giống Update =====
            if (skill == QuestionSkill.Listening && string.IsNullOrWhiteSpace(model.MediaUrl))
            {
                context.AddFailure(nameof(CreateQuestionBankCommand.MediaUrl), "Câu hỏi Listening bắt buộc phải có MediaUrl.");
            }

            if (skill == QuestionSkill.Reading && string.IsNullOrWhiteSpace(model.Content))
            {
                context.AddFailure(nameof(CreateQuestionBankCommand.Content), "Câu hỏi Reading bắt buộc phải có Content.");
            }

            // ===== Options rules (giữ như cũ) =====
            if (skill == QuestionSkill.Writing)
            {
                if (model.Options != null && model.Options.Any())
                {
                    context.AddFailure(nameof(CreateQuestionBankCommand.Options), AppErrors.WritingNoOptions.Description);
                }
                return;
            }

            if (model.Options == null || model.Options.Count < 2 || model.Options.Count > 4)
            {
                context.AddFailure(nameof(CreateQuestionBankCommand.Options), AppErrors.QuestionBankInvalidOptions.Description);
                return;
            }

            var validKeys = new HashSet<string> { "1", "2", "3", "4" };
            var keys = new List<string>();
            var correctCount = 0;

            foreach (var o in model.Options)
            {
                if (o == null)
                {
                    context.AddFailure(nameof(CreateQuestionBankCommand.Options), "Danh sách đáp án chứa phần tử không hợp lệ.");
                    continue;
                }

                var key = o.KeyOption?.Trim();
                if (string.IsNullOrWhiteSpace(key) || !validKeys.Contains(key))
                {
                    context.AddFailure(nameof(CreateQuestionBankCommand.Options), AppErrors.QuestionBankInvalidKeyOption.Description);
                }
                else
                {
                    keys.Add(key);
                }

                var hasText = !string.IsNullOrWhiteSpace(o.Content);
                var hasImage = !string.IsNullOrWhiteSpace(o.ImageUrl);
                if (!hasText && !hasImage)
                {
                    context.AddFailure(nameof(CreateQuestionBankCommand.Options), "Đáp án phải có nội dung text hoặc ảnh.");
                }

                if (o.IsCorrect) correctCount++;
            }

            if (keys.Distinct().Count() != keys.Count)
            {
                context.AddFailure(nameof(CreateQuestionBankCommand.Options), AppErrors.QuestionBankDuplicateKeyOption.Description);
            }

            if (correctCount == 0)
            {
                context.AddFailure(nameof(CreateQuestionBankCommand.Options), AppErrors.QuestionBankNoCorrectAnswer.Description);
            }
            else if (correctCount > 1)
            {
                context.AddFailure(nameof(CreateQuestionBankCommand.Options), AppErrors.QuestionBankMultipleCorrectAnswers.Description);
            }
        }
    }
}
