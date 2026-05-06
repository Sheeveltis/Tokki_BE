using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
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
        private readonly IRedisService _redisService;
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailService _emailService;
        private readonly IValidator<SendEmailVerificationOtpCommand> _validator;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private string messFail = "Gửi OTP thất bại.";

        public SendGeneralOtpCommandHandler(
            IRedisService redisService,
            IAccountRepository accountRepository,
            IEmailService emailService,
            IValidator<SendEmailVerificationOtpCommand> validator,
            ISystemConfigRepository systemConfigRepository)
        {
            _redisService = redisService;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _validator = validator;
            _systemConfigRepository = systemConfigRepository;
        }

        public async Task<OperationResult<string>> Handle(SendEmailVerificationOtpCommand request, CancellationToken cancellationToken)
        {
            // Kiểm tra tài khoản đã tồn tại chưa
            var existingAccount = await _accountRepository.GetByEmailAsync(request.Email);
            if (existingAccount != null)
            {
                bool isBannedOrDeleted = existingAccount.Status == AccountStatus.Inactive ||
                                         existingAccount.Status == AccountStatus.Banned;

                if (isBannedOrDeleted)
                    return OperationResult<string>.Failure(AppErrors.AccountUnavailable, 400, messFail);

                return OperationResult<string>.Failure(AppErrors.EmailAlreadyExists, 400, messFail);
            }

            // Rate Limit: kiểm tra key OTP_RL:VerifyEmail:{email} có tồn tại không
            var rateLimitKey = $"OTP_RL:VerifyEmail:{request.Email}";
            var rateLimitEntry = await _redisService.GetAsync(rateLimitKey);
            if (rateLimitEntry != null)
            {
                var ttl = await _redisService.GetTtlAsync(rateLimitKey);
                int remainingSeconds = ttl.HasValue ? (int)Math.Ceiling(ttl.Value.TotalSeconds) : 60;
                return OperationResult<string>.Failure(
                    AppErrors.OtpRateLimitExceeded(remainingSeconds),
                    StatusCodes.Status429TooManyRequests,
                    messFail
                );
            }

            string? configValue = await _systemConfigRepository.GetValueByKeyAsync("OTP_EXPIRATION_SECONDS");
            int otpLifeTimeSeconds = 300;
            if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue, out int cfgResult))
                otpLifeTimeSeconds = cfgResult;

            var otpCode = new Random().Next(100000, 999999).ToString();

            // Lưu OTP vào Redis
            var otpKey = $"OTP:VerifyEmail:{request.Email}";
            var otpValue = JsonSerializer.Serialize(new { OtpCode = otpCode, AttemptCount = 0 });
            await _redisService.SetAsync(otpKey, otpValue, TimeSpan.FromSeconds(otpLifeTimeSeconds));

            // Lưu rate-limit key 60 giây
            await _redisService.SetAsync(rateLimitKey, "1", TimeSpan.FromSeconds(60));

            string subject = "[Tokki] Mã xác thực email";
            string body = $@"
            <h3>Xin chào,</h3>
            <p>Bạn vừa yêu cầu xác thực email tại Tokki.</p>
            <p>Mã xác thực (OTP) của bạn là: <b style='font-size: 20px; color: blue;'>{otpCode}</b></p>
            <p>Mã này sẽ hết hạn sau {otpLifeTimeSeconds / 60} phút.</p>
            <p>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email.</p>";

            // CHẠY NGẦM: Để tránh dính lỗi Cloudflare 524 Timeout
            // API sẽ trả về 200 OK ngay lập tức, việc gửi email sẽ tự động chạy trong nền
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailAsync(request.Email, subject, body);
                }
                catch (Exception)
                {
                    // Nếu gửi lỗi, do chạy ngầm nên không văng exception ra API
                    // Ở môi trường thực tế, nên dùng ILogger để ghi lại lỗi này.
                }
            });

            return OperationResult<string>.Success("Mã OTP đã được gửi đến email của bạn.", 200);
        }
    }
}