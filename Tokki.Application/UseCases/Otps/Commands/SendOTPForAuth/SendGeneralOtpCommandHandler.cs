using FluentValidation;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Otps.Commands.SendGeneralOtp
{
    public class SendGeneralOtpCommandHandler
        : IRequestHandler<SendGeneralOtpCommand, OperationResult<string>>
    {
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly IValidator<SendGeneralOtpCommand> _validator;

        public SendGeneralOtpCommandHandler(
            IOtpRepository otpRepository,
            IEmailService emailService,
            IValidator<SendGeneralOtpCommand> validator)
        {
            _otpRepository = otpRepository;
            _emailService = emailService;
            _validator = validator;
        }

        public async Task<OperationResult<string>> Handle(
            SendGeneralOtpCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Validate bằng FluentValidation
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return OperationResult<string>.Failure(errorMessages, 400);
            }

            // 2. Tạo mã OTP ngẫu nhiên
            var otpCode = new Random().Next(100000, 999999).ToString();

            // 3. Tạo Entity OTP (Type = General)
            var otpEntity = new Otp
            {
                Email = request.Email,
                OtpCode = otpCode,
                Type = OtpType.General,
                Status = OtpStatus.Active,
                UserId = null,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                ExpiredAt = DateTime.UtcNow.AddHours(7).AddMinutes(5)
            };

            // 4. Lưu vào DB
            await _otpRepository.AddAsync(otpEntity);
            await _otpRepository.SaveChangesAsync(cancellationToken);

            // 5. Gửi email OTP
            string subject = "Mã xác thực (General)";
            string body =
                $"<h3>Mã xác thực của bạn là: " +
                $"<b style='color:blue; font-size:20px;'>{otpCode}</b></h3>" +
                $"<p>Mã có hiệu lực trong 5 phút.</p>";

            await _emailService.SendEmailAsync(request.Email, subject, body);

            return OperationResult<string>.Success("Đã gửi OTP thành công (General).", 200);
        }
    }
}
