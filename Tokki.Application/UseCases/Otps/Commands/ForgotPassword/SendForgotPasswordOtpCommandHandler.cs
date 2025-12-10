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
        private readonly IIdGeneratorService _idGenerator; // ✅ Thêm dòng này

        public SendForgotPasswordOtpCommandHandler(
            IAccountRepository accountRepository,
            IOtpRepository otpRepository,
            IEmailService emailService,
            ISystemConfigRepository systemConfigRepo,
            IIdGeneratorService idGenerator) // ✅ Thêm parameter
        {
            _accountRepository = accountRepository;
            _otpRepository = otpRepository;
            _emailService = emailService;
            _systemConfigRepo = systemConfigRepo;
            _idGenerator = idGenerator; // ✅ Thêm dòng này
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
            var newOtp = new Otp
            {
                OtpId = _idGenerator.Generate(15), // ✅ Tạo NanoID 15 ký tự
                Email = user.Email,
                OtpCode = otpCode,
                Type = OtpType.ResetPassword,
                Status = OtpStatus.Active,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                ExpiredAt = DateTime.UtcNow.AddHours(7).AddSeconds(lifeTime)
            };

            await _otpRepository.AddAsync(newOtp);
            await _otpRepository.SaveChangesAsync(cancellationToken);

            await _emailService.SendEmailAsync(user.Email, "Mã khôi phục mật khẩu",
                $"Mã OTP của bạn là: <b>{otpCode}</b>. Mã hết hạn sau {lifeTime / 60} phút.");

            return OperationResult<string>.Success("Mã OTP đã được gửi.", 200);
        }
    }
}