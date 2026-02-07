using FluentValidation;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.VerifyEmailOtp
{
    public class VerifyEmailOtpCommandHandler : IRequestHandler<VerifyEmailOtpCommand, OperationResult<string>>
    {
        private readonly IOtpRepository _otpRepository;
        private readonly ISystemConfigRepository _systemConfigRepository;

        public VerifyEmailOtpCommandHandler(
            IOtpRepository otpRepository,
            ISystemConfigRepository systemConfigRepository
            )
        {
            _otpRepository = otpRepository;
            _systemConfigRepository = systemConfigRepository;
            
        }

        public async Task<OperationResult<string>> Handle(VerifyEmailOtpCommand request, CancellationToken cancellationToken)
        {
            // Lấy Config Retry Limit
            string? configValue = await _systemConfigRepository.GetValueByKeyAsync("OTP_RETRY_LIMIT");
            int maxRetryLimit = 5;
            if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue, out int result))
            {
                maxRetryLimit = result;
            }

            // Tìm OTP 
            var validOtp = await _otpRepository.GetLatestValidOtpAsync(request.Email, OtpType.VerifyEmail);

            if (validOtp == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpNotFound });
            }

            DateTime currentTime = DateTime.UtcNow.AddHours(7);

            // Kiểm tra hết hạn
            if (validOtp.ExpiredAt < currentTime)
            {
                validOtp.Status = OtpStatus.Expired;
                await _otpRepository.UpdateAsync(validOtp);
                await _otpRepository.SaveChangesAsync(cancellationToken);
                return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpExpired });
            }

            // Kiểm tra Max Retry 
            if (validOtp.AttemptCount >= maxRetryLimit)
            {
                if (validOtp.Status == OtpStatus.Active)
                {
                    validOtp.Status = OtpStatus.Revoked;
                    await _otpRepository.UpdateAsync(validOtp);
                    await _otpRepository.SaveChangesAsync(cancellationToken);
                }
                return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpMaxRetryExceeded });
            }

            // So sánh Code
            if (validOtp.OtpCode != request.OtpCode)
            {
                validOtp.AttemptCount++;

                if (validOtp.AttemptCount >= maxRetryLimit)
                {
                    validOtp.Status = OtpStatus.Revoked;
                }

                await _otpRepository.UpdateAsync(validOtp);
                await _otpRepository.SaveChangesAsync(cancellationToken);

                int remainingAttempts = maxRetryLimit - validOtp.AttemptCount;
                if (remainingAttempts <= 0)
                {
                    return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpRevoked });
                }

                // Trường hợp này cần dynamic message, giữ nguyên hoặc tạo Error mới với placeholder
                return OperationResult<string>.Failure($"Mã xác thực không chính xác. Bạn còn {remainingAttempts} lần thử.", 400);
            }

            // Xử lý khi đúng
            validOtp.Status = OtpStatus.Used;
            validOtp.UsedAt = currentTime;

            await _otpRepository.UpdateAsync(validOtp);
            await _otpRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success("Xác thực email thành công!", 200);
        }
    }
}