using FluentValidation;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories; // Chứa IOtpRepository và IAccountRepository
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Otps.Commands.SendOtpForEmailVerification;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.SendEmailVerificationOtp
{
    public class SendEmailVerificationOtpCommandHandler : IRequestHandler<SendEmailVerificationOtpCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly IValidator<SendEmailVerificationOtpCommand> _validator;
        private readonly ISystemConfigRepository _systemConfigRepository;


        public SendEmailVerificationOtpCommandHandler(
            IAccountRepository accountRepository,
            IOtpRepository otpRepository,
            IEmailService emailService,
            IValidator<SendEmailVerificationOtpCommand> validator,
            ISystemConfigRepository systemConfigRepository)
        {
            _accountRepository = accountRepository;
            _otpRepository = otpRepository;
            _emailService = emailService;
            _validator = validator;
            _systemConfigRepository = systemConfigRepository;
        }

        public async Task<OperationResult<string>> Handle(SendEmailVerificationOtpCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate Email đầu vào
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return OperationResult<string>.Failure(errorMessages, 400);
            }

            // 2. Kiểm tra User có tồn tại không (Dùng AccountRepository)
            var user = await _accountRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                // Bảo mật: Có thể trả về "Đã gửi OTP" dù email không tồn tại để tránh dò user, 
                // nhưng ở đây mình báo lỗi 404 cho dễ debug.
                return OperationResult<string>.Failure("Email này chưa được đăng ký tài khoản.", 404);
            }

            // 3. Kiểm tra trạng thái tài khoản
            if (user.Status == AccountStatus.Banned)
            {
                return OperationResult<string>.Failure("Tài khoản đã bị vô hiệu hóa.", 403);
            }

            // 4. (Tùy chọn) Rate Limit: Kiểm tra xem có OTP nào còn hạn vừa gửi không?
            // Bạn có thể dùng hàm GetLatestValidOtpAsync để chặn spam nếu muốn.
            // Ví dụ: var existingOtp = await _otpRepository.GetLatestValidOtpAsync(user.Email, OtpType.Login);
            // if (existingOtp != null && (DateTime.UtcNow - existingOtp.CreatedAt).TotalSeconds < 60) 
            //      return Failure("Vui lòng đợi 1 phút trước khi gửi lại.");

            // 5. Tạo OTP Code mới
            var otpCode = new Random().Next(100000, 999999).ToString();

            string? configValue = await _systemConfigRepository.GetValueByKeyAsync("OTP_EXPIRATION_SECONDS");

            int otpLifeTimeSeconds = 300;

            // 3. Ép kiểu từ string sang int (Cần kiểm tra null và format hợp lệ)
            if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue, out int result))
            {
                otpLifeTimeSeconds = result;
            }
            // 6. Tạo Entity Otp
            var newOtp = new Otp
            {
                Email = user.Email,
                OtpCode = otpCode,
                Type = OtpType.VerifyEmail,

                // Giữ nguyên logic +7 của bạn
                CreatedAt = DateTime.UtcNow.AddHours(7),
                ExpiredAt = DateTime.UtcNow.AddHours(7).AddSeconds(otpLifeTimeSeconds),

                // SỬA LỖI: Thay IsUsed = false bằng Status
                Status = OtpStatus.Active,

                // Khởi tạo các giá trị mặc định khác (nếu cần thiết để rõ ràng)
                AttemptCount = 0,
                UsedAt = null
            };
            // 7. Lưu vào DB thông qua Repository
            await _otpRepository.AddAsync(newOtp);
            await _otpRepository.SaveChangesAsync(cancellationToken);

            // 8. Gửi Email
            try
            {
                // Nội dung email
                string subject = "[Tokki] Mã xác thực đăng nhập";
                string body = $@"
                    <h3>Xin chào {user.FullName},</h3>
                    <p>Bạn vừa yêu cầu đăng nhập vào Tokki.</p>
                    <p>Mã xác thực (OTP) của bạn là: <b style='font-size: 20px; color: blue;'>{otpCode}</b></p>
                    <p>Mã này sẽ hết hạn sau 5 phút.</p>
                    <p>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email.</p>";

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception)
            {
                // Nếu gửi mail lỗi thì coi như thất bại, dù đã lưu DB
                return OperationResult<string>.Failure("Hệ thống gửi mail đang gặp sự cố. Vui lòng thử lại sau.", 500);
            }

            return OperationResult<string>.Success("Mã OTP đã được gửi đến email của bạn.", 200);
        }
    }
}