using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Otps.Commands.VerifyForgotPasswordOtp
{
    // DTO nội bộ để deserialize value trong Redis
    internal class ForgotPasswordOtpRedisEntry
    {
        public string OtpCode { get; set; } = string.Empty;
        public int AttemptCount { get; set; } = 0;
    }

    public class VerifyForgotPasswordOtpCommandHandler : IRequestHandler<VerifyForgotPasswordOtpCommand, OperationResult<string>>
    {
        private readonly IRedisService _redisService;
        private readonly IAccountRepository _accountRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public VerifyForgotPasswordOtpCommandHandler(
            IRedisService redisService,
            IAccountRepository accountRepository,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _redisService = redisService;
            _accountRepository = accountRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<OperationResult<string>> Handle(VerifyForgotPasswordOtpCommand request, CancellationToken cancellationToken)
        {
            // Tìm OTP trong Redis
            var otpKey = $"OTP:ResetPassword:{request.Email}";
            var rawValue = await _redisService.GetAsync(otpKey);

            if (rawValue == null)
                return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpInvalid });

            var entry = JsonSerializer.Deserialize<ForgotPasswordOtpRedisEntry>(rawValue);
            if (entry == null)
                return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpInvalid });

            // So sánh mã OTP
            if (entry.OtpCode != request.OtpCode)
                return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpCodeWrong });

            // Đúng → xóa key (one-time use) và phát token reset
            await _redisService.DeleteAsync(otpKey);

            string resetToken = _jwtTokenGenerator.GenerateForgotPasswordToken(request.Email);
            return OperationResult<string>.Success(resetToken, 200, "Xác thực thành công. Dùng Token này để đặt lại mật khẩu.");
        }
    }
}