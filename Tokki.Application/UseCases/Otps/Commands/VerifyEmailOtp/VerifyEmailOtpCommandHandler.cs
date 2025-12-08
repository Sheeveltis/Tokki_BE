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
        private readonly IValidator<VerifyEmailOtpCommand> _validator;

        public VerifyEmailOtpCommandHandler(
            IOtpRepository otpRepository,
            ISystemConfigRepository systemConfigRepository,
            IValidator<VerifyEmailOtpCommand> validator)
        {
            _otpRepository = otpRepository;
            _systemConfigRepository = systemConfigRepository;
            _validator = validator;
        }

        public async Task<OperationResult<string>> Handle(VerifyEmailOtpCommand request, CancellationToken cancellationToken)
        {
            // --- 1. Validate Input ---
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return OperationResult<string>.Failure(errorMessages, 400);
            }

            // --- 2. Lấy Config Retry Limit ---
            string? configValue = await _systemConfigRepository.GetValueByKeyAsync("OTP_RETRY_LIMIT");
            int maxRetryLimit = 5;
            if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue, out int result))
            {
                maxRetryLimit = result;
            }

            // --- 3. Tìm OTP trong DB ---
            var validOtp = await _otpRepository.GetLatestValidOtpAsync(request.Email, OtpType.VerifyEmail);

            if (validOtp == null)
            {
                return OperationResult<string>.Failure("Mã xác thực không tồn tại hoặc đã hết hạn.", 400);
            }

            // Lấy giờ hiện tại (UTC+7)
            DateTime currentTime = DateTime.UtcNow.AddHours(7);

            // --- 4. Kiểm tra hết hạn (Lazy Check) ---
            if (validOtp.ExpiredAt < currentTime)
            {
                validOtp.Status = OtpStatus.Expired;
                await _otpRepository.UpdateAsync(validOtp);
                await _otpRepository.SaveChangesAsync(cancellationToken);
                return OperationResult<string>.Failure("Mã xác thực đã hết hạn.", 400);
            }

            // --- 5. Kiểm tra Max Retry ---
            if (validOtp.AttemptCount >= maxRetryLimit)
            {
                if (validOtp.Status == OtpStatus.Active)
                {
                    validOtp.Status = OtpStatus.Revoked;
                    await _otpRepository.UpdateAsync(validOtp);
                    await _otpRepository.SaveChangesAsync(cancellationToken);
                }
                return OperationResult<string>.Failure("Bạn đã nhập sai quá số lần quy định. Mã xác thực đã bị hủy.", 400);
            }

            // --- 6. So sánh Code ---
            if (validOtp.OtpCode != request.OtpCode)
            {
                // XỬ LÝ KHI SAI
                validOtp.AttemptCount++;

                // Nếu chạm ngưỡng -> Hủy luôn
                if (validOtp.AttemptCount >= maxRetryLimit)
                {
                    validOtp.Status = OtpStatus.Revoked;
                }

                await _otpRepository.UpdateAsync(validOtp);
                await _otpRepository.SaveChangesAsync(cancellationToken);

                int remainingAttempts = maxRetryLimit - validOtp.AttemptCount;
                if (remainingAttempts <= 0)
                {
                    return OperationResult<string>.Failure("Bạn đã nhập sai quá số lần quy định. Mã này đã bị khóa.", 400);
                }

                return OperationResult<string>.Failure($"Mã xác thực không chính xác. Bạn còn {remainingAttempts} lần thử.", 400);
            }

            // --- 7. XỬ LÝ KHI ĐÚNG ---
            validOtp.Status = OtpStatus.Used;
            validOtp.UsedAt = currentTime;


            // 6.1. Cập nhật OTP
            validOtp.Status = OtpStatus.Used;      // Thay IsUsed = true bằng Enum Used
            validOtp.UsedAt = currentTime;         // Lưu thời gian UTC+7

 
            await _otpRepository.UpdateAsync(validOtp);
            await _otpRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success("Xác thực email thành công!", 200);
        }
    }
}