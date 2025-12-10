using FluentValidation;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Otps.Commands.SendOtpForEmailVerification;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.SendEmailVerificationOtp
{
    public class SendGeneralOtpCommandHandler : IRequestHandler<SendEmailVerificationOtpCommand, OperationResult<string>>
    {
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly IValidator<SendEmailVerificationOtpCommand> _validator;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IIdGeneratorService _idGenerator; 

        public SendGeneralOtpCommandHandler(
            IOtpRepository otpRepository,
            IEmailService emailService,
            IValidator<SendEmailVerificationOtpCommand> validator,
            ISystemConfigRepository systemConfigRepository,
            IIdGeneratorService idGenerator) 
        {
            _otpRepository = otpRepository;
            _emailService = emailService;
            _validator = validator;
            _systemConfigRepository = systemConfigRepository;
            _idGenerator = idGenerator; 
        }

        public async Task<OperationResult<string>> Handle(SendEmailVerificationOtpCommand request, CancellationToken cancellationToken)
        {
            // Rate Limit Check
            var existingOtp = await _otpRepository.GetLatestValidOtpAsync(request.Email, OtpType.VerifyEmail);
            if (existingOtp != null)
            {
                var timeSinceLastOtp = (DateTime.UtcNow.AddHours(7) - existingOtp.CreatedAt).TotalSeconds;
                if (timeSinceLastOtp < 60)
                {
                    return OperationResult<string>.Failure(
                        $"Vui lòng đợi {60 - (int)timeSinceLastOtp} giây trước khi gửi lại.",
                        429);
                }
            }

            var otpCode = new Random().Next(100000, 999999).ToString();

            string? configValue = await _systemConfigRepository.GetValueByKeyAsync("OTP_EXPIRATION_SECONDS");
            int otpLifeTimeSeconds = 300;

            if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue, out int result))
            {
                otpLifeTimeSeconds = result;
            }

            var newOtp = new Otp
            {
                OtpId = _idGenerator.Generate(15), 
                Email = request.Email,
                OtpCode = otpCode,
                Type = OtpType.VerifyEmail,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                ExpiredAt = DateTime.UtcNow.AddHours(7).AddSeconds(otpLifeTimeSeconds),
                Status = OtpStatus.Active,
                AttemptCount = 0,
                UsedAt = null
            };

            await _otpRepository.AddAsync(newOtp);
            await _otpRepository.SaveChangesAsync(cancellationToken);

            try
            {
                string subject = "[Tokki] Mã xác thực email";
                string body = $@"
            <h3>Xin chào,</h3>
            <p>Bạn vừa yêu cầu xác thực email tại Tokki.</p>
            <p>Mã xác thực (OTP) của bạn là: <b style='font-size: 20px; color: blue;'>{otpCode}</b></p>
            <p>Mã này sẽ hết hạn sau {otpLifeTimeSeconds / 60} phút.</p>
            <p>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email.</p>";

                await _emailService.SendEmailAsync(request.Email, subject, body);
            }
            catch (Exception)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.EmailServiceError });
            }

            return OperationResult<string>.Success("Mã OTP đã được gửi đến email của bạn.", 200);
        }
    }
}