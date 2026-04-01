using FluentValidation;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.Login { 
    public class LoginCommandHandler : IRequestHandler<LoginCommand, OperationResult<LoginResponse>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IValidator<LoginCommand> _validator;
        private readonly IGamificationService _gamificationService;
        private readonly IEmailHistoryRepository _emailHistoryRepository;
        private readonly IRefreshTokenService _refreshTokenService;

        public LoginCommandHandler(
            IAccountRepository accountRepository,
            ISystemConfigRepository systemConfigRepository,
            IJwtTokenGenerator jwtGenerator,
            IIdGeneratorService idGenerator,
            IGamificationService gamificationService,
            IValidator<LoginCommand> validator,
            IEmailHistoryRepository emailHistoryRepository,
            IRefreshTokenService refreshTokenService)
        {
            _accountRepository = accountRepository;
            _systemConfigRepository = systemConfigRepository;
            _jwtGenerator = jwtGenerator;
            _idGenerator = idGenerator;
            _validator = validator;
            _gamificationService = gamificationService;
            _emailHistoryRepository = emailHistoryRepository;
            _refreshTokenService = refreshTokenService;
        }

        public async Task<OperationResult<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByEmailAsync(request.Email);

            // 1. Kiểm tra tồn tại
            if (user == null)
                return OperationResult<LoginResponse>.Failure(new List<Error> { AppErrors.UserNotFound }, 404, "Tài khoản không tồn tại.");

            // 1.1. Kiểm tra quyền truy cập (nếu có yêu cầu nhóm role cụ thể)
            if (request.AllowedRoles != null && request.AllowedRoles.Count > 0)
            {
                if (!request.AllowedRoles.Contains(user.Role))
                {
                    return OperationResult<LoginResponse>.Failure("Bạn không có quyền đăng nhập vào hệ thống này.", 403);
                }
            }

            DateTime utcNow = DateTime.UtcNow;
            DateTime vietnamTimeNow = utcNow.AddHours(7);

            // 2. Kiểm tra trạng thái tài khoản
            if (user.Status == AccountStatus.Inactive)
                return OperationResult<LoginResponse>.Failure(new List<Error> { AppErrors.AccountInActive }, 403, "Tài khoản của bạn không hoạt động.");

            if (user.Status == AccountStatus.Banned)
                return OperationResult<LoginResponse>.Failure(new List<Error> { AppErrors.AccountBanned }, 403, "Tài khoản của bạn đã bị khóa vĩnh viễn.");

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
                return OperationResult<LoginResponse>.Failure(new List<Error> { AppErrors.WrongPassword }, 400, AppErrors.WrongPassword.Description);
            }

            // 4.2. Chặn nếu đang dùng mật khẩu mặc định
            if (await IsUsingAnyDefaultPasswordAsync(request.Password, cancellationToken))
                return OperationResult<LoginResponse>.Failure(new List<Error> { AppErrors.DefaultPasswordUsed }, 403, AppErrors.DefaultPasswordUsed.Description);

            // === XỬ LÝ KHI ĐÚNG MẬT KHẨU ===
            if (user.FailedLoginCount > 0 || user.LockedUntil != null)
            {
                user.FailedLoginCount = 0;
                user.LockedUntil = null;
            }

            await _gamificationService.CheckLoginGamificationAsync(user);

            user.LastLoginAt = vietnamTimeNow;
            user.UpdatedAt = vietnamTimeNow;

            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            // --- 5. TẠO ACCESS TOKEN ---
            int tokenExpirationMinutes = await GetIntConfig("TOKEN_EXPIRATION_MINUTES", 60);
            DateTime tokenExpiresAtUtc = utcNow.AddMinutes(tokenExpirationMinutes);
            var accessToken = _jwtGenerator.GenerateToken(user, tokenExpiresAtUtc);

            await _emailHistoryRepository.DeleteByUserAndTemplateTypeAsync(
                user.UserId,
                EmailTemplateType.OfflineReminder,
                cancellationToken);

            await _accountRepository.SaveChangesAsync(cancellationToken);

            // --- 6. XỬ LÝ REFRESH TOKEN (Remember Me) ---
            string? rawRefreshToken = null;

            if (request.RememberMe)
            {
                // Thu hồi tất cả token cũ → chỉ giữ 1 session duy nhất
                await _refreshTokenService.RevokeAllRefreshTokensAsync(user.UserId);
                // Tạo refresh token mới, lưu hash vào DB
                rawRefreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user);
            }

            var response = new LoginResponse
            {
                Token = accessToken,
                RefreshToken = rawRefreshToken, // null nếu không tick Remember Me
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
            return (!string.IsNullOrEmpty(val) && int.TryParse(val, out int result)) ? result : defaultValue;
        }

        private async Task<bool> IsUsingAnyDefaultPasswordAsync(string inputPassword, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(inputPassword))
                return false;

            var keys = new[] { "DEFAULT_PASSWORD_FOR_STAFF", "DEFAULT_PASSWORD_FOR_USER", "DEFAULT_PASSWORD_FOR_ADMIN" };

            foreach (var key in keys)
            {
                var v = await _systemConfigRepository.GetValueByKeyAsync(key);
                if (!string.IsNullOrEmpty(v) && inputPassword == v)
                    return true;
            }

            return false;
        }
    }
}