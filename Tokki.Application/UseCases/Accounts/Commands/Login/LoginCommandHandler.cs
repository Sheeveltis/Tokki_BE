using FluentValidation;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.Login;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Queries.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, OperationResult<LoginResponse>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ISystemConfigRepository _systemConfigRepository; // 1. Thêm Repo Config
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IValidator<LoginCommand> _validator;

        public LoginCommandHandler(
            IAccountRepository accountRepository,
            ISystemConfigRepository systemConfigRepository, // 2. Inject vào Constructor
            IJwtTokenGenerator jwtGenerator,
            IIdGeneratorService idGenerator,
            IValidator<LoginCommand> validator)
        {
            _accountRepository = accountRepository;
            _systemConfigRepository = systemConfigRepository;
            _jwtGenerator = jwtGenerator;
            _idGenerator = idGenerator;
            _validator = validator;
        }

        public async Task<OperationResult<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // --- 1. Validate Input ---
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return OperationResult<LoginResponse>.Failure(errorMessages, 400);
            }

            // --- 2. Tìm User ---
            var user = await _accountRepository.GetByEmailAsync(request.Email);

            // YÊU CẦU: Báo lỗi rõ ràng nếu không tìm thấy user
            if (user == null)
            {
                return OperationResult<LoginResponse>.Failure("Tài khoản không tồn tại.", 404);
            }

            // --- 3. Kiểm tra trạng thái khóa (Lockout Check) ---
            DateTime currentTime = DateTime.UtcNow.AddHours(7); // Đồng bộ giờ VN

            if (user.Status == AccountStatus.Banned)
            {
                return OperationResult<LoginResponse>.Failure("Tài khoản của bạn đã bị khóa vĩnh viễn. Vui lòng liên hệ CSKH.", 403);
            }

            if (user.LockedUntil.HasValue && user.LockedUntil > currentTime)
            {
                var remainingMinutes = Math.Ceiling((user.LockedUntil.Value - currentTime).TotalMinutes);
                return OperationResult<LoginResponse>.Failure($"Tài khoản đang bị tạm khóa. Vui lòng thử lại sau {remainingMinutes} phút.", 403);
            }

            // --- 4. Kiểm tra mật khẩu ---
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                // === XỬ LÝ KHI SAI MẬT KHẨU ===
                await HandleFailedLoginAsync(user, currentTime, cancellationToken);

                // YÊU CẦU: Báo lỗi cụ thể là sai mật khẩu
                return OperationResult<LoginResponse>.Failure("Mật khẩu không chính xác.", 400);
            }

            // === XỬ LÝ KHI ĐÚNG MẬT KHẨU (Reset bộ đếm) ===
            if (user.FailedLoginCount > 0 || user.LockedUntil != null)
            {
                user.FailedLoginCount = 0;
                user.LockedUntil = null;
                // Không cần save ngay vì đoạn dưới có save session, EF sẽ track cả user nếu chung context
                // Tuy nhiên để chắc chắn ta update user luôn
                await _accountRepository.UpdateUserAsync(user); // Cần đảm bảo Repo có hàm này
            }

            // --- 5. Logic tạo Token & Session (Giữ nguyên) ---
            var accessToken = _jwtGenerator.GenerateToken(user);
            var tokenExpiryTime = currentTime.AddMinutes(120);

            var newSession = new Session
            {
                SessionId = _idGenerator.Generate(10),
                UserId = user.UserId,
                RefreshToken = accessToken,
                ExpiresAt = tokenExpiryTime,
                IpAddress = "Check-Controller",
                UserAgent = "Web-Browser",
                CreatedAt = currentTime,
                RevokedAt = null
            };

            await _accountRepository.AddSessionAsync(newSession);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            var response = new LoginResponse
            {
                Token = accessToken,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl ?? "default-avatar"
            };

            return OperationResult<LoginResponse>.Success(response, 200, "Đăng nhập thành công!");
        }

        // --- Hàm phụ trợ xử lý Logic khóa ---
        private async Task HandleFailedLoginAsync(Account user, DateTime currentTime, CancellationToken token)
        {
            // 1. Tăng số lần sai
            user.FailedLoginCount++;

            // 2. Lấy cấu hình từ DB
            int limit = await GetIntConfig("LOGIN_FAILED_LIMIT", 5);

            // 3. Logic khóa theo cấp độ
            // Level 1: Sai bằng giới hạn (ví dụ 5 lần) -> Khóa 5 phút
            if (user.FailedLoginCount == limit)
            {
                int duration = await GetIntConfig("LOGIN_LOCKOUT_DURATION_LEVEL_1", 300); // 300s
                user.LockedUntil = currentTime.AddSeconds(duration);
            }
            // Level 2: Sai gấp đôi giới hạn (ví dụ 10 lần) -> Khóa 30 phút
            else if (user.FailedLoginCount == limit * 2)
            {
                int duration = await GetIntConfig("LOGIN_LOCKOUT_DURATION_LEVEL_2", 1800); // 1800s
                user.LockedUntil = currentTime.AddSeconds(duration);
            }
            // Level 3: Sai gấp 3 giới hạn (ví dụ 15 lần) -> Khóa vĩnh viễn
            else if (user.FailedLoginCount >= limit * 3)
            {
                // Kiểm tra xem config có phải là PERMANENT_LOCK không
                string? action = await _systemConfigRepository.GetValueByKeyAsync("LOGIN_LOCKOUT_LEVEL_3_ACTION");
                if (action == "PERMANENT_LOCK")
                {
                    user.Status = AccountStatus.Banned;
                    user.LockedUntil = null; // Đã Ban thì ko cần LockedUntil nữa
                }
            }

            // 4. Lưu User xuống DB
            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(token);
        }

        // Helper lấy config int
        private async Task<int> GetIntConfig(string key, int defaultValue)
        {
            string? val = await _systemConfigRepository.GetValueByKeyAsync(key);
            if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int result))
            {
                return result;
            }
            return defaultValue;
        }
    }
}