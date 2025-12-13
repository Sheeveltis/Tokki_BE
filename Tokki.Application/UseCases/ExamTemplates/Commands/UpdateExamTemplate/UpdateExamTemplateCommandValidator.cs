using FluentValidation;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate
{
    public class UpdateExamTemplateCommandValidator : AbstractValidator<UpdateExamTemplateCommand>
    {
        public UpdateExamTemplateCommandValidator()
        {
            // 1. ExamTemplateId
            // Sử dụng: NotEmptyValidator
            RuleFor(x => x.ExamTemplateId)
                .NotEmpty()
                .WithName("ID mẫu đề thi");

            // 2. Name
            // Sử dụng: NotEmptyValidator, MaximumLengthValidator
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100)
                .WithName("Tên mẫu đề thi");

            // 3. Description
            // Sử dụng: MaximumLengthValidator
            RuleFor(x => x.Description)
                .MaximumLength(255)
                .When(x => !string.IsNullOrEmpty(x.Description))
                .WithName("Mô tả");

            // 4. Status
            // Sử dụng: EnumValidator
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithName("Trạng thái");

            // 5. Parts (Danh sách)
            // Sử dụng: NotEmptyValidator
            // Thông báo lỗi sẽ là: "'Danh sách phần thi' không được để trống."
            RuleFor(x => x.Parts)
                .NotEmpty()
                .WithName("Danh sách phần thi");

            // 6. Chi tiết từng phần trong Parts
            RuleForEach(x => x.Parts).ChildRules(part =>
            {
                // Skill
                // Sử dụng: EnumValidator
                part.RuleFor(p => p.Skill)
                    .IsInEnum()
                    .WithName("Kỹ năng");

                // QuestionFrom
                // Sử dụng: GreaterThanValidator
                // Thông báo lỗi: "'Câu bắt đầu' phải lớn hơn '0'."
                part.RuleFor(p => p.QuestionFrom)
                    .GreaterThan(0)
                    .WithName("Câu bắt đầu");

                // QuestionTo
                // Sử dụng: GreaterThanValidator
                // Thông báo lỗi: "'Câu kết thúc' phải lớn hơn '0'."
                part.RuleFor(p => p.QuestionTo)
                    .GreaterThan(0)
                    .WithName("Câu kết thúc");

                // So sánh QuestionTo >= QuestionFrom
                // Sử dụng: GreaterThanOrEqualValidator
                // Thông báo lỗi: "'Câu kết thúc' phải lớn hơn hoặc bằng '{Giá trị của Câu bắt đầu}'."
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
}