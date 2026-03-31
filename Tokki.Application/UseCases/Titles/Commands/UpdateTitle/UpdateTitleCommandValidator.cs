using FluentValidation;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Titles.Commands.UpdateTitle
{
    public class UpdateTitleCommandValidator : AbstractValidator<UpdateTitleCommand>
    {
        public UpdateTitleCommandValidator()
        {
            RuleFor(x => x.TitleId)
                .NotEmpty().WithMessage("TitleId không được để trống.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên danh hiệu không được để trống.")
                .MaximumLength(100).WithMessage("Tên danh hiệu không được vượt quá 100 ký tự.");

            RuleFor(x => x.ColorHex)
                .NotEmpty().WithMessage("Mã màu HEX không được để trống.")
                .Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$").WithMessage("Mã màu HEX không hợp lệ (VD: #FFFFFF).");

            RuleFor(x => x.IconUrl)
                .NotEmpty().WithMessage("URL Icon không được để trống.");

            RuleFor(x => x.RequirementQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Giá trị điều kiện không được âm.");

            RuleFor(x => x.RequirementType)
                .IsInEnum().WithMessage("Loại điều kiện không hợp lệ.");
        }
    }
}
