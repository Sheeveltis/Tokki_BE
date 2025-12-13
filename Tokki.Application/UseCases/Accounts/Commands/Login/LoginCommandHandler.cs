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
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IValidator<LoginCommand> _validator;
        private readonly IGamificationService _gamificationService;

        public LoginCommandHandler(
            IAccountRepository accountRepository,
            ISystemConfigRepository systemConfigRepository,
            IJwtTokenGenerator jwtGenerator,
            IIdGeneratorService idGenerator,
            IGamificationService gamificationService,
            IValidator<LoginCommand> validator)
        {
            _accountRepository = accountRepository;
            _systemConfigRepository = systemConfigRepository;
            _jwtGenerator = jwtGenerator;
            _idGenerator = idGenerator;
            _validator = validator;
            _gamificationService = gamificationService;
        }

        public async Task<OperationResult<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByEmailAsync(request.Email);

            // 1. Kiểm tra tồn tại
            if (user == null)
            {
                return OperationResult<LoginResponse>.Failure(new List<Error> { AppErrors.UserNotFound }, 404, "Tài khoản không tồn tại.");
            }

            // Lấy thời gian hiện tại
            DateTime utcNow = DateTime.UtcNow;
            DateTime vietnamTimeNow = utcNow.AddHours(7);

            // 2. Kiểm tra Banned
            if (user.Status == AccountStatus.Banned)
            {
                return OperationResult<LoginResponse>.Failure(new List<Error> { AppErrors.AccountBanned }, 403, "Tài khoản của bạn đã bị khóa vĩnh viễn.");
            }

            // 3. Kiểm tra Locked (Tạm khóa)
            if (user.LockedUntil.HasValue && user.LockedUntil > vietnamTimeNow)
            {
                var remainingMinutes = Math.Ceiling((user.LockedUntil.Value - vietnamTimeNow).TotalMinutes);
                return OperationResult<LoginResponse>.Failure(new List<Error> { AppErrors.AccountLocked }, 403, $"Tài khoản đang bị tạm khóa. Thử lại sau {remainingMinutes} phút.");
            }

            // 4. Kiểm tra mật khẩu
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                await HandleFailedLoginAsync(user, vietnamTimeNow, cancellationToken);
                return OperationResult<LoginResponse>.Failure(new List<Error> { AppErrors.WrongPassword }, 400, "Mật khẩu không chính xác.");
            }

            // === XỬ LÝ KHI ĐÚNG MẬT KHẨU ===
            if (user.FailedLoginCount > 0 || user.LockedUntil != null)
            {
                user.FailedLoginCount = 0;
                user.LockedUntil = null;
                await _accountRepository.UpdateUserAsync(user);
            }

            await _gamificationService.CheckLoginGamificationAsync(user.UserId);

            // --- 5. TẠO TOKEN & SESSION ---

            // A. Lấy config thời gian hết hạn từ database (mặc định 60 phút)
            int tokenExpirationMinutes = await GetIntConfig("TOKEN_EXPIRATION_MINUTES", 60);

            // B. Tính toán thời gian hết hạn
            // - Token JWT: Dùng UTC (chuẩn quốc tế, tránh lỗi validate)
            // - Database: Lưu giờ Việt Nam (+7) để dễ quản lý
            DateTime tokenExpiresAtUtc = utcNow.AddMinutes(tokenExpirationMinutes);
            DateTime sessionExpiresAtVietnam = vietnamTimeNow.AddMinutes(tokenExpirationMinutes);

            // C. Tạo JWT Token (truyền UTC)
            var accessToken = _jwtGenerator.GenerateToken(user, tokenExpiresAtUtc);

            // D. Lưu Session vào database (lưu giờ Việt Nam)
            var newSession = new Session
            {
                SessionId = _idGenerator.Generate(10),
                UserId = user.UserId,
                RefreshToken = accessToken, // Có thể tạo refresh token riêng
                ExpiresAt = sessionExpiresAtVietnam, // Lưu giờ Việt Nam (+7)
                IpAddress = "Check-Controller", // Nên lấy từ HttpContext
                UserAgent = "Web-Browser",
                CreatedAt = vietnamTimeNow, // Lưu giờ Việt Nam (+7)
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

        // --- Hàm phụ trợ ---
        private async Task HandleFailedLoginAsync(Account user, DateTime currentTime, CancellationToken token)
        {
            user.FailedLoginCount++;
            int limit = await GetIntConfig("LOGIN_FAILED_LIMIT", 5);

            if (user.FailedLoginCount == limit)
            {
                int duration = await GetIntConfig("LOGIN_LOCKOUT_DURATION_LEVEL_1", 300);
                user.LockedUntil = currentTime.AddSeconds(duration);
            }
            else if (user.FailedLoginCount == limit * 2)
            {
                int duration = await GetIntConfig("LOGIN_LOCKOUT_DURATION_LEVEL_2", 1800);
                user.LockedUntil = currentTime.AddSeconds(duration);
            }
            else if (user.FailedLoginCount >= limit * 3)
            {
                string? action = await _systemConfigRepository.GetValueByKeyAsync("LOGIN_LOCKOUT_LEVEL_3_ACTION");
                if (action == "PERMANENT_LOCK")
                {
                    user.Status = AccountStatus.Banned;
                    user.LockedUntil = null;
                }
            }

            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(token);
        }

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