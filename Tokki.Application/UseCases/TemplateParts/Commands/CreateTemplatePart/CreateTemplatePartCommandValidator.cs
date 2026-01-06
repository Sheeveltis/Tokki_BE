using FluentValidation;
using Tokki.Application.UseCases.TemplateParts.Commands.CreateTemplatePart;

namespace Tokki.Application.UseCases.TemplateParts.Commands.CreateTemplatePart
{
    public class CreateTemplatePartCommandValidator : AbstractValidator<CreateTemplatePartCommand>
    {
        public CreateTemplatePartCommandValidator()
        {
            RuleFor(x => x.ExamTemplateId).NotEmpty();

            RuleFor(x => x.Skill).IsInEnum().WithName("Kỹ năng");

            RuleFor(x => x.QuestionFrom).GreaterThan(0).WithName("Câu bắt đầu");
            RuleFor(x => x.QuestionTo).GreaterThan(0).WithName("Câu kết thúc");
            RuleFor(x => x.QuestionTo).GreaterThanOrEqualTo(x => x.QuestionFrom).WithName("Câu kết thúc");

            RuleFor(x => x.PartTitle).NotEmpty().MaximumLength(150).WithName("Tiêu đề phần");

            // Validate mới cho Mark
            RuleFor(x => x.Mark).GreaterThan(0).WithName("Điểm số");

            RuleFor(x => x.QuestionTypeId).NotEmpty().WithName("Loại câu hỏi");
        }
    }
}