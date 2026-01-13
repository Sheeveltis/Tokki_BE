using FluentValidation;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank
{
    public class UpdateQuestionBankCommandValidator : AbstractValidator<UpdateQuestionBankCommand>
    {
        private readonly IQuestionTypeRepository _questionTypeRepository;
        private readonly IQuestionBankRepository _questionBankRepository;

        public UpdateQuestionBankCommandValidator(
            IQuestionTypeRepository questionTypeRepository,
            IQuestionBankRepository questionBankRepository)
        {
            _questionTypeRepository = questionTypeRepository;
            _questionBankRepository = questionBankRepository;

            RuleFor(x => x.QuestionBankId)
                .NotEmpty()
                .WithName("Mã câu hỏi");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithName("Nội dung câu hỏi");

            RuleFor(x => x).CustomAsync(ValidateBusinessRulesAsync);
        }

        private async Task ValidateBusinessRulesAsync(
            UpdateQuestionBankCommand model,
            ValidationContext<UpdateQuestionBankCommand> context,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(model.QuestionBankId))
                return;

            var qb = await _questionBankRepository.GetByIdAsync(model.QuestionBankId.Trim(), cancellationToken);
            if (qb == null)
            {
                context.AddFailure(nameof(UpdateQuestionBankCommand.QuestionBankId), AppErrors.QuestionBankNotFound.Description);
                return;
            }

            // old skill
            QuestionSkill? oldSkill = null;
            if (!string.IsNullOrWhiteSpace(qb.QuestionTypeId))
            {
                var oldQt = await _questionTypeRepository.GetByIdAsync(qb.QuestionTypeId.Trim(), cancellationToken);
                oldSkill = oldQt?.Skill;
            }

            // final question type id
            var finalQuestionTypeId = string.IsNullOrWhiteSpace(model.QuestionTypeId)
                ? qb.QuestionTypeId
                : model.QuestionTypeId.Trim();

            var newQt = await _questionTypeRepository.GetByIdAsync(finalQuestionTypeId.Trim(), cancellationToken);
            if (newQt == null)
            {
                context.AddFailure(nameof(UpdateQuestionBankCommand.QuestionTypeId), AppErrors.QuestionTypeNotFound.Description);
                return;
            }

            if (!newQt.IsActive)
            {
                context.AddFailure(nameof(UpdateQuestionBankCommand.QuestionTypeId), "Loại câu hỏi đang bị vô hiệu hóa.");
                return;
            }

            var newSkill = newQt.Skill;

            // Rule 7: nghe phải có MediaUrl
            if (newSkill == QuestionSkill.Listening)
            {
                if (string.IsNullOrWhiteSpace(model.MediaUrl))
                {
                    context.AddFailure(nameof(UpdateQuestionBankCommand.MediaUrl), "Câu hỏi Listening bắt buộc phải có MediaUrl.");
                }
            }

            // Rule 8: đọc phải có content (đã có RuleFor Content NotEmpty; giữ thêm nếu bạn muốn tách message)
            if (newSkill == QuestionSkill.Reading)
            {
                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    context.AddFailure(nameof(UpdateQuestionBankCommand.Content), "Câu hỏi Reading bắt buộc phải có Content.");
                }
            }

            // Rule 6: viết không được có đáp án
            if (newSkill == QuestionSkill.Writing)
            {
                if (model.Options != null && model.Options.Any())
                {
                    context.AddFailure(nameof(UpdateQuestionBankCommand.Options), AppErrors.WritingNoOptions.Description);
                }
            }

            // Rule 2: đọc->viết và nghe->đọc phải truyền rỗng để xóa đáp án cũ
            var isListeningToReading = oldSkill.HasValue && oldSkill.Value == QuestionSkill.Listening && newSkill == QuestionSkill.Reading;
            var isReadingToWriting = oldSkill.HasValue && oldSkill.Value == QuestionSkill.Reading && newSkill == QuestionSkill.Writing;

            if (isListeningToReading || isReadingToWriting)
            {
                if (model.Options != null && model.Options.Any())
                {
                    context.AddFailure(nameof(UpdateQuestionBankCommand.Options),
                        isListeningToReading
                            ? "Nghe sang Đọc: Options phải truyền rỗng để xóa toàn bộ đáp án cũ."
                            : "Đọc sang Viết: Options phải truyền rỗng để xóa toàn bộ đáp án cũ."
                    );
                }
            }

            // Rule 3: nghe->đọc phải có content (đã NotEmpty; vẫn thêm message rõ)
            if (isListeningToReading)
            {
                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    context.AddFailure(nameof(UpdateQuestionBankCommand.Content), "Nghe sang Đọc: Content là bắt buộc.");
                }
            }

            // Rule 4: viết->đọc phải có content (đã NotEmpty; vẫn thêm message rõ)
            if (oldSkill.HasValue && oldSkill.Value == QuestionSkill.Writing && newSkill == QuestionSkill.Reading)
            {
                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    context.AddFailure(nameof(UpdateQuestionBankCommand.Content), "Viết sang Đọc: Content là bắt buộc.");
                }
            }

            // Rule 5: viết->nghe phải có MediaUrl
            if (oldSkill.HasValue && oldSkill.Value == QuestionSkill.Writing && newSkill == QuestionSkill.Listening)
            {
                if (string.IsNullOrWhiteSpace(model.MediaUrl))
                {
                    context.AddFailure(nameof(UpdateQuestionBankCommand.MediaUrl), "Viết sang Nghe: MediaUrl là bắt buộc.");
                }
            }

            // Options validation cho trường hợp final skill là Reading/Listening:
            // - Nếu Listening->Reading: bắt buộc rỗng theo rule, nên bỏ qua validate options
            // - Nếu final skill != Writing và không phải Listening->Reading: enforce 2-4 options + key + đúng 1 đáp án đúng
            if (newSkill != QuestionSkill.Writing && !isListeningToReading)
            {
                if (model.Options == null || model.Options.Count < 2 || model.Options.Count > 4)
                {
                    context.AddFailure(nameof(UpdateQuestionBankCommand.Options), AppErrors.QuestionBankInvalidOptions.Description);
                    return;
                }

                var validKeys = new HashSet<string> { "1", "2", "3", "4" };
                var keys = new List<string>();
                var correctCount = 0;

                foreach (var o in model.Options)
                {
                    if (o == null)
                    {
                        context.AddFailure(nameof(UpdateQuestionBankCommand.Options), "Danh sách đáp án chứa phần tử không hợp lệ.");
                        continue;
                    }

                    var key = o.KeyOption?.Trim();
                    if (string.IsNullOrWhiteSpace(key) || !validKeys.Contains(key))
                    {
                        context.AddFailure(nameof(UpdateQuestionBankCommand.Options), AppErrors.QuestionBankInvalidKeyOption.Description);
                    }
                    else
                    {
                        keys.Add(key);
                    }

                    var hasText = !string.IsNullOrWhiteSpace(o.Content);
                    var hasImage = !string.IsNullOrWhiteSpace(o.ImageUrl);
                    if (!hasText && !hasImage)
                    {
                        context.AddFailure(nameof(UpdateQuestionBankCommand.Options), "Đáp án phải có nội dung text hoặc ảnh.");
                    }

                    if (o.IsCorrect) correctCount++;
                }

                if (keys.Distinct().Count() != keys.Count)
                {
                    context.AddFailure(nameof(UpdateQuestionBankCommand.Options), AppErrors.QuestionBankDuplicateKeyOption.Description);
                }

                if (correctCount == 0)
                {
                    context.AddFailure(nameof(UpdateQuestionBankCommand.Options), AppErrors.QuestionBankNoCorrectAnswer.Description);
                }
                else if (correctCount > 1)
                {
                    context.AddFailure(nameof(UpdateQuestionBankCommand.Options), AppErrors.QuestionBankMultipleCorrectAnswers.Description);
                }
            }
        }
    }
}
