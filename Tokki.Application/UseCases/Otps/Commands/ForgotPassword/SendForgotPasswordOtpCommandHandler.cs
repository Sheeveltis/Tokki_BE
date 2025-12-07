using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly ISystemConfigRepository _systemConfigRepo;

        public SendForgotPasswordOtpCommandHandler(
            IAccountRepository accountRepository, IOtpRepository otpRepository,
            IEmailService emailService, ISystemConfigRepository systemConfigRepo)
        {
            _accountRepository = accountRepository;
            _otpRepository = otpRepository;
            _emailService = emailService;
            _systemConfigRepo = systemConfigRepo;
        }

        public async Task<OperationResult<string>> Handle(SendForgotPasswordOtpCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra Email
            var user = await _accountRepository.GetByEmailAsync(request.Email);
            if (user == null) return OperationResult<string>.Failure("Email không tồn tại.", 404);
            if (user.Status == AccountStatus.Banned) return OperationResult<string>.Failure("Tài khoản đã bị khóa.", 403);

            // 2. Lấy thời gian hết hạn OTP (Mặc định 300s)
            string? configValue = await _systemConfigRepo.GetValueByKeyAsync("OTP_EXPIRATION_SECONDS");
            int lifeTime = 300;
            if (int.TryParse(configValue, out int result)) lifeTime = result;

            // 3. Tạo OTP
            var otpCode = new Random().Next(100000, 999999).ToString();
            var newOtp = new Otp
            {
                Email = user.Email,
                OtpCode = otpCode,
                Type = OtpType.ResetPassword, // <--- Dùng đúng Enum của bạn
                Status = OtpStatus.Active,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                ExpiredAt = DateTime.UtcNow.AddHours(7).AddSeconds(lifeTime)
            };

            await _otpRepository.AddAsync(newOtp);
            await _otpRepository.SaveChangesAsync(cancellationToken);

            // 4. Gửi Email
            await _emailService.SendEmailAsync(user.Email, "Mã khôi phục mật khẩu",
                $"Mã OTP của bạn là: <b>{otpCode}</b>. Mã hết hạn sau {lifeTime / 60} phút.");

            return OperationResult<string>.Success("Mã OTP đã được gửi.", 200);
        }
    }
}
