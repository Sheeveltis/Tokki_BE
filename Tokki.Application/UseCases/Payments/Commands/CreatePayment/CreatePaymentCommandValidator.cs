using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.Payments.Commands.CreatePayment
{
    public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
    {
        public CreatePaymentCommandValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(1000)
                .WithMessage("Số tiền thanh toán phải lớn hơn 1000 VNĐ.");
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithName("ID người dùng");
        }
    }
}
