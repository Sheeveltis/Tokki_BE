using FluentValidation;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate
{
    public class CreateExamTemplateCommandValidator : AbstractValidator<CreateExamTemplateCommand>
    {
        public CreateExamTemplateCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithName("Tên mẫu đề thi");
            RuleFor(x => x.Description).MaximumLength(255).WithName("Mô tả");
            RuleFor(x => x.Type).IsInEnum().WithName("Loại đề");

            // Validate Parts
            RuleFor(x => x.Parts).NotEmpty().WithName("Danh sách phần thi");
            RuleForEach(x => x.Parts).ChildRules(part =>
            {
                part.RuleFor(p => p.Skill).IsInEnum().WithName("Kỹ năng");
                part.RuleFor(p => p.QuestionFrom).GreaterThan(0).WithName("Câu bắt đầu");
                part.RuleFor(p => p.QuestionTo).GreaterThan(0).WithName("Câu kết thúc");
                part.RuleFor(p => p.QuestionTo).GreaterThanOrEqualTo(p => p.QuestionFrom).WithName("Câu kết thúc");
                part.RuleFor(p => p.PartTitle).MaximumLength(255).WithName("Tiêu đề phần");
                part.RuleFor(p => p.Mark).GreaterThan(0).WithName("Điểm số");
                part.RuleFor(p => p.QuestionTypeId).NotEmpty().WithName("Loại câu hỏi");
            });
        }
    }
}