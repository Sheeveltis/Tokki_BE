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
        private readonly IIdGeneratorService _idGenerator; 

        public SendGeneralOtpCommandHandler(
            IOtpRepository otpRepository,
            IEmailService emailService,
            IIdGeneratorService idGenerator)
        {
            _otpRepository = otpRepository;
            _emailService = emailService;
            _idGenerator = idGenerator; 
        }

        public async Task<OperationResult<string>> Handle(
            SendGeneralOtpCommand request,
            CancellationToken cancellationToken)
        {
            // Tạo mã OTP ngẫu nhiên
            var otpCode = new Random().Next(100000, 999999).ToString();

            // Tạo Entity OTP (Type = General)
            var otpEntity = new Otp
            {
                OtpId = _idGenerator.Generate(15), 
                Email = request.Email,
                OtpCode = otpCode,
                Type = OtpType.General,
                Status = OtpStatus.Active,
                UserId = null,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                ExpiredAt = DateTime.UtcNow.AddHours(7).AddMinutes(5)
            };

            await _otpRepository.AddAsync(otpEntity);
            await _otpRepository.SaveChangesAsync(cancellationToken);

            // Gửi email 
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