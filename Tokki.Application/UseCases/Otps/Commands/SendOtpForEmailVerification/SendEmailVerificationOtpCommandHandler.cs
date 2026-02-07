using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
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
        private readonly IAccountRepository _accountRepository; // 1. Thêm Repository Account
        private readonly IEmailService _emailService;
        private readonly IValidator<SendEmailVerificationOtpCommand> _validator;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IIdGeneratorService _idGenerator;
        private string messFail =  "Gửi OTP thất bại.";

        public SendGeneralOtpCommandHandler(
            IOtpRepository otpRepository,
            IAccountRepository accountRepository, // 2. Inject vào Constructor
            IEmailService emailService,
            IValidator<SendEmailVerificationOtpCommand> validator,
            ISystemConfigRepository systemConfigRepository,
            IIdGeneratorService idGenerator)
        {
            _otpRepository = otpRepository;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _validator = validator;
            _systemConfigRepository = systemConfigRepository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<string>> Handle(SendEmailVerificationOtpCommand request, CancellationToken cancellationToken)
        {
          
            var existingAccount = await _accountRepository.GetByEmailAsync(request.Email);

            if (existingAccount != null)
            {
                bool isBannedOrDeleted = existingAccount.Status == AccountStatus.Inactive ||
                                         existingAccount.Status == AccountStatus.Banned; // Ví dụ enum Banned

                if (isBannedOrDeleted)
                {
                    // Trường hợp bị xóa hoặc ban -> Báo lỗi liên hệ quản trị viên
                    return OperationResult<string>.Failure(AppErrors.AccountUnavailable, 400, messFail);
                }

                // Trường hợp tài khoản đang hoạt động bình thường -> Báo lỗi đã đăng ký
                return OperationResult<string>.Failure(AppErrors.EmailAlreadyExists, 400, messFail);
            }

            // ---------------------------------------------

            // Rate Limit Check
            var existingOtp = await _otpRepository.GetLatestValidOtpAsync(request.Email, OtpType.VerifyEmail);
            if (existingOtp != null)
            {
                var timeSinceLastOtp =
    (DateTime.UtcNow.AddHours(7) - existingOtp.CreatedAt).TotalSeconds;

                const int OTP_RESEND_SECONDS = 60;

                if (timeSinceLastOtp < OTP_RESEND_SECONDS)
                {
                    var remainingSeconds =
                        Math.Max(0, OTP_RESEND_SECONDS - (int)timeSinceLastOtp);

                    return OperationResult<string>.Failure(
                        AppErrors.OtpRateLimitExceeded(remainingSeconds),
                        StatusCodes.Status429TooManyRequests,
                        messFail
                    );
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
                // Nếu gửi mail lỗi, có thể bạn muốn xóa record OTP vừa tạo để tránh rác DB (tùy chọn)
                return OperationResult<string>.Failure(new List<Error> { AppErrors.EmailServiceError },400, messFail);
            }

            return OperationResult<string>.Success("Mã OTP đã được gửi đến email của bạn.", 200);
        }
    }
}