using FluentValidation;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBankByStaff;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBankByStaff
{
    public class CreateQuestionBankByStaffCommandValidator : AbstractValidator<CreateQuestionBankByStaffCommand>
    {
        private readonly IQuestionTypeRepository _questionTypeRepository;

        public CreateQuestionBankByStaffCommandValidator(IQuestionTypeRepository questionTypeRepository)
        {
            _questionTypeRepository = questionTypeRepository;

            RuleFor(x => x.QuestionTypeId)
                .NotEmpty()
                .WithName("Loại câu hỏi");

            // Validate theo Skill trong DB
            RuleFor(x => x).CustomAsync(ValidateBySkillAsync);
        }

        private async Task ValidateBySkillAsync(
            CreateQuestionBankByStaffCommand model,
            ValidationContext<CreateQuestionBankByStaffCommand> context,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(model.QuestionTypeId))
                return;

            var questionTypeId = model.QuestionTypeId.Trim();

            var questionType = await _questionTypeRepository.GetByIdAsync(questionTypeId, cancellationToken);
            if (questionType == null)
            {
                context.AddFailure(nameof(CreateQuestionBankByStaffCommand.QuestionTypeId),
                    AppErrors.QuestionTypeNotFound.Description);
                return;
            }

            if (!questionType.IsActive)
            {
                context.AddFailure(nameof(CreateQuestionBankByStaffCommand.QuestionTypeId),
                    "Loại câu hỏi đang bị vô hiệu hóa.");
                return;
            }

            var skill = questionType.Skill;

            // ===== Skill rules giống Update/Create =====
            if (skill == QuestionSkill.Listening && string.IsNullOrWhiteSpace(model.MediaUrl))
            {
                context.AddFailure(nameof(CreateQuestionBankByStaffCommand.MediaUrl),
                    "Câu hỏi Listening bắt buộc phải có MediaUrl.");
            }

            if (skill == QuestionSkill.Reading && string.IsNullOrWhiteSpace(model.Content))
            {
                context.AddFailure(nameof(CreateQuestionBankByStaffCommand.Content),
                    "Câu hỏi Reading bắt buộc phải có Content.");
            }

            // ===== Options rules =====
            if (skill == QuestionSkill.Writing)
            {
                if (model.Options != null && model.Options.Any())
                {
                    context.AddFailure(nameof(CreateQuestionBankByStaffCommand.Options),
                        AppErrors.WritingNoOptions.Description);
                }
                return;
            }

            // Reading/Listening: bắt buộc 2-4 options
            if (model.Options == null || model.Options.Count < 2 || model.Options.Count > 4)
            {
                context.AddFailure(nameof(CreateQuestionBankByStaffCommand.Options),
                    AppErrors.QuestionBankInvalidOptions.Description);
                return;
            }

            var validKeys = new HashSet<string> { "1", "2", "3", "4" };
            var keys = new List<string>();
            var correctCount = 0;

            foreach (var o in model.Options)
            {
                if (o == null)
                {
                    context.AddFailure(nameof(CreateQuestionBankByStaffCommand.Options),
                        "Danh sách đáp án chứa phần tử không hợp lệ.");
                    continue;
                }

                var key = o.KeyOption?.Trim();
                if (string.IsNullOrWhiteSpace(key) || !validKeys.Contains(key))
                {
                    context.AddFailure(nameof(CreateQuestionBankByStaffCommand.Options),
                        AppErrors.QuestionBankInvalidKeyOption.Description);
                }
                else
                {
                    keys.Add(key);
                }

                var hasText = !string.IsNullOrWhiteSpace(o.Content);
                var hasImage = !string.IsNullOrWhiteSpace(o.ImageUrl);
                if (!hasText && !hasImage)
                {
                    context.AddFailure(nameof(CreateQuestionBankByStaffCommand.Options),
                        "Đáp án phải có nội dung text hoặc ảnh.");
                }

                if (o.IsCorrect) correctCount++;
            }

            if (keys.Distinct().Count() != keys.Count)
            {
                context.AddFailure(nameof(CreateQuestionBankByStaffCommand.Options),
                    AppErrors.QuestionBankDuplicateKeyOption.Description);
            }

            if (correctCount == 0)
            {
                context.AddFailure(nameof(CreateQuestionBankByStaffCommand.Options),
                    AppErrors.QuestionBankNoCorrectAnswer.Description);
            }
            else if (correctCount > 1)
            {
                context.AddFailure(nameof(CreateQuestionBankByStaffCommand.Options),
                    AppErrors.QuestionBankMultipleCorrectAnswers.Description);
            }
        }
    }
}
