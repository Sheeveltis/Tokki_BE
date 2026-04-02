using FluentValidation;
using MediatR;
using System.Text.Json;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.VerifyEmailOtp
{
    // DTO nội bộ để deserialize value trong Redis
    internal class OtpRedisEntry
    {
        public string OtpCode { get; set; } = string.Empty;
        public int AttemptCount { get; set; } = 0;
    }

    public class VerifyEmailOtpCommandHandler : IRequestHandler<VerifyEmailOtpCommand, OperationResult<string>>
    {
        private readonly IRedisService _redisService;
        private readonly ISystemConfigRepository _systemConfigRepository;

        public VerifyEmailOtpCommandHandler(
            IRedisService redisService,
            ISystemConfigRepository systemConfigRepository)
        {
            _redisService = redisService;
            _systemConfigRepository = systemConfigRepository;
        }

        public async Task<OperationResult<string>> Handle(VerifyEmailOtpCommand request, CancellationToken cancellationToken)
        {
            // Lấy Retry Limit từ config
            string? configValue = await _systemConfigRepository.GetValueByKeyAsync("OTP_RETRY_LIMIT");
            int maxRetryLimit = 5;
            if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue, out int cfgResult))
                maxRetryLimit = cfgResult;

            // Tìm OTP trong Redis
            var otpKey = $"OTP:VerifyEmail:{request.Email}";
            var rawValue = await _redisService.GetAsync(otpKey);

            if (rawValue == null)
                return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpNotFound });

            var entry = JsonSerializer.Deserialize<OtpRedisEntry>(rawValue);
            if (entry == null)
                return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpNotFound });

            // Kiểm tra đã vượt max retry (đã bị revoke trước đó)
            if (entry.AttemptCount >= maxRetryLimit)
                return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpMaxRetryExceeded });

            // So sánh OTP
            if (entry.OtpCode != request.OtpCode)
            {
                entry.AttemptCount++;

                if (entry.AttemptCount >= maxRetryLimit)
                {
                    // Revoke: xóa key khỏi Redis
                    await _redisService.DeleteAsync(otpKey);
                    return OperationResult<string>.Failure(new List<Error> { AppErrors.OtpRevoked });
                }

                // Cập nhật AttemptCount, giữ nguyên TTL còn lại
                var remainingTtl = await _redisService.GetTtlAsync(otpKey);
                var ttlToUse = remainingTtl.HasValue && remainingTtl.Value.TotalSeconds > 0
                    ? remainingTtl.Value
                    : TimeSpan.FromSeconds(300);

                var updatedValue = JsonSerializer.Serialize(entry);
                await _redisService.SetAsync(otpKey, updatedValue, ttlToUse);

                int remainingAttempts = maxRetryLimit - entry.AttemptCount;
                return OperationResult<string>.Failure($"Mã xác thực không chính xác. Bạn còn {remainingAttempts} lần thử.", 400);
            }

            // Đúng OTP → xóa key (one-time use)
            await _redisService.DeleteAsync(otpKey);
            return OperationResult<string>.Success("Xác thực email thành công!", 200);
        }
    }
}
