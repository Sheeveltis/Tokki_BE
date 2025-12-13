using FluentValidation;
using Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate;

public class CreateExamTemplateCommandValidator : AbstractValidator<CreateExamTemplateCommand>
{
    public CreateExamTemplateCommandValidator()
    {
        // 1. Name
        // Sử dụng: NotEmptyValidator, MaximumLengthValidator
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithName("Tên mẫu đề thi");

        // 2. Description
        // Sử dụng: MaximumLengthValidator
        RuleFor(x => x.Description)
            .MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithName("Mô tả");

        // 3. Parts (Danh sách)
        // Sử dụng: NotEmptyValidator (Thông báo sẽ là: 'Danh sách phần thi' không được để trống.)
        RuleFor(x => x.Parts)
            .NotEmpty()
            .WithName("Danh sách phần thi");

        // 4. Các rule con trong Parts
        RuleForEach(x => x.Parts).ChildRules(part =>
        {
            // Skill
            // Sử dụng: EnumValidator
            part.RuleFor(p => p.Skill)
                .IsInEnum()
                .WithName("Kỹ năng");

            // QuestionFrom
            // Sử dụng: GreaterThanValidator
            part.RuleFor(p => p.QuestionFrom)
                .GreaterThan(0)
                .WithName("Câu bắt đầu");

            // QuestionTo
            // Sử dụng: GreaterThanValidator
            part.RuleFor(p => p.QuestionTo)
                .GreaterThan(0)
                .WithName("Câu kết thúc");

            // Logic QuestionTo >= QuestionFrom
            // Sử dụng: GreaterThanOrEqualValidator
            // Thông báo sẽ là: 'Câu kết thúc' phải lớn hơn hoặc bằng '{Giá trị của QuestionFrom}'.
            part.RuleFor(p => p.QuestionTo)
                .GreaterThanOrEqualTo(p => p.QuestionFrom)
                .WithName("Câu kết thúc");

            // PartTitle
            // Sử dụng: MaximumLengthValidator
            part.RuleFor(p => p.PartTitle)
                .MaximumLength(255)
                .When(p => !string.IsNullOrEmpty(p.PartTitle))
                .WithName("Tiêu đề phần");

            // ExampleType
            // Sử dụng: EnumValidator
            part.RuleFor(p => p.ExampleType)
                .IsInEnum()
                .WithName("Loại ví dụ");
        });
    }
}