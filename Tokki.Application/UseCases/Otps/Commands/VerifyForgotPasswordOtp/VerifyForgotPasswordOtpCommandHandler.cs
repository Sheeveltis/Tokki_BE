using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Otps.Commands.VerifyForgotPasswordOtp
{
    // UseCases/Accounts/Commands/ForgotPassword/VerifyOtp/VerifyForgotPasswordOtpCommandHandler.cs
    public class VerifyForgotPasswordOtpCommandHandler : IRequestHandler<VerifyForgotPasswordOtpCommand, OperationResult<string>>
    {
        private readonly IOtpRepository _otpRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        

        public VerifyForgotPasswordOtpCommandHandler(IOtpRepository otpRepository, IAccountRepository accountRepository, IJwtTokenGenerator jwtTokenGenerator)
        {
            _otpRepository = otpRepository;
            _accountRepository = accountRepository;
            _jwtTokenGenerator= jwtTokenGenerator;

        }

        public async Task<OperationResult<string>> Handle(VerifyForgotPasswordOtpCommand request, CancellationToken cancellationToken)
        {
            // 1. Logic check OTP (Giữ nguyên như cũ)
            var validOtp = await _otpRepository.GetLatestValidOtpAsync(request.Email, OtpType.ResetPassword);

            if (validOtp == null) return OperationResult<string>.Failure("OTP không hợp lệ.", 400);
            if (validOtp.OtpCode != request.OtpCode) return OperationResult<string>.Failure("Sai mã OTP.", 400);

            // 2. Đánh dấu OTP đã dùng (Để ko dùng lại)
            validOtp.Status = OtpStatus.Used;
            validOtp.UsedAt = DateTime.UtcNow.AddHours(7);
            await _otpRepository.UpdateAsync(validOtp);
            await _otpRepository.SaveChangesAsync(cancellationToken);

            // 3. TẠO JWT TOKEN CHỈ CHỨA EMAIL (Logic mới)
            string resetToken = _jwtTokenGenerator.GenerateForgotPasswordToken(request.Email);

            // 4. Trả về Token cho Client
            return OperationResult<string>.Success(resetToken, 200, "Xác thực thành công. Dùng Token này để đặt lại mật khẩu.");
        }
    }
}
