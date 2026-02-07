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
    public class VerifyForgotPasswordOtpCommandHandler : IRequestHandler<VerifyForgotPasswordOtpCommand, OperationResult<string>>
    {
        private readonly IOtpRepository _otpRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public VerifyForgotPasswordOtpCommandHandler(IOtpRepository otpRepository, IAccountRepository accountRepository, IJwtTokenGenerator jwtTokenGenerator)
        {
            _otpRepository = otpRepository;
            _accountRepository = accountRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<OperationResult<string>> Handle(VerifyForgotPasswordOtpCommand request, CancellationToken cancellationToken)
        {
            // Logic check OTP 
            var validOtp = await _otpRepository.GetLatestValidOtpAsync(request.Email, OtpType.ResetPassword);
            if (validOtp == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpInvalid });
            }

            if (validOtp.OtpCode != request.OtpCode)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpCodeWrong });
            }

            // Đánh dấu OTP đã dùng
            validOtp.Status = OtpStatus.Used;
            validOtp.UsedAt = DateTime.UtcNow.AddHours(7);
            await _otpRepository.UpdateAsync(validOtp);
            await _otpRepository.SaveChangesAsync(cancellationToken);

            string resetToken = _jwtTokenGenerator.GenerateForgotPasswordToken(request.Email);

            return OperationResult<string>.Success(resetToken, 200, "Xác thực thành công. Dùng Token này để đặt lại mật khẩu.");
        }
    }
}