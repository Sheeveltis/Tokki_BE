using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json; // Added for JSON serialization
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Otps.Commands.ForgotPassword
{
    public class SendForgotPasswordOtpCommandHandler : IRequestHandler<SendForgotPasswordOtpCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IRedisService _redisService;
        private readonly IEmailService _emailService;
        private readonly ISystemConfigRepository _systemConfigRepo;

        public SendForgotPasswordOtpCommandHandler(
            IAccountRepository accountRepository,
            IRedisService redisService,
            IEmailService emailService,
            ISystemConfigRepository systemConfigRepo)
        {
            _accountRepository = accountRepository;
            _redisService = redisService;
            _emailService = emailService;
            _systemConfigRepo = systemConfigRepo;
        }

        public async Task<OperationResult<string>> Handle(SendForgotPasswordOtpCommand request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.UserNotFound });
            }

            if (user.Status == AccountStatus.Banned)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.AccountBanned });
            }

            string? configValue = await _systemConfigRepo.GetValueByKeyAsync("OTP_EXPIRATION_SECONDS");
            int lifeTime = 300;
            if (int.TryParse(configValue, out int result)) lifeTime = result;

            var otpCode = new Random().Next(100000, 999999).ToString();
            
            var redisKey = $"OTP:ResetPassword:{user.Email}";
            var redisValue = JsonSerializer.Serialize(new { OtpCode = otpCode, AttemptCount = 0 });
            await _redisService.SetAsync(redisKey, redisValue, TimeSpan.FromSeconds(lifeTime));

            await _emailService.SendEmailAsync(user.Email, "Mã khôi phục mật khẩu",
                $"Mã OTP của bạn là: <b>{otpCode}</b>. Mã hết hạn sau {lifeTime / 60} phút.");

            return OperationResult<string>.Success("Mã OTP đã được gửi.", 200);
        }
    }
}