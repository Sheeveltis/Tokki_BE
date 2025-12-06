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
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IValidator<LoginCommand> _validator;

        public LoginCommandHandler(
            IAccountRepository accountRepository,
            IJwtTokenGenerator jwtGenerator,
            IIdGeneratorService idGenerator,
            IValidator<LoginCommand> validator) 
        {
            _accountRepository = accountRepository;
            _jwtGenerator = jwtGenerator;
            _idGenerator = idGenerator;
            _validator = validator; 
        }

        public async Task<OperationResult<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                // Gom tất cả lỗi lại thành 1 chuỗi để trả về
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));

                return OperationResult<LoginResponse>.Failure(errorMessages, 400);
            }


            // 1. Tìm User thông qua Repository
            var user = await _accountRepository.GetByEmailAsync(request.Email);

            if (user == null)
            {
                return OperationResult<LoginResponse>.Failure("Tài khoản hoặc mật khẩu không chính xác.", 400);
            }

            // 2. Kiểm tra mật khẩu bằng BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return OperationResult<LoginResponse>.Failure("Tài khoản hoặc mật khẩu không chính xác.", 400);
            }

            // 3. Kiểm tra trạng thái
            if (user.Status == AccountStatus.Banned)
            {
                return OperationResult<LoginResponse>.Failure("Tài khoản của bạn đã bị vô hiệu hóa.", 403);
            }

            // 4. Tạo JWT Token
            var accessToken = _jwtGenerator.GenerateToken(user);

            // Thời gian hết hạn của Token
            var tokenExpiryTime = DateTime.UtcNow.AddMinutes(120); // Sửa lại 120 phút cho hợp lý

            // 5. Tạo Session Entity
            var newSession = new Session
            {
                SessionId = _idGenerator.Generate(10),
                UserId = user.UserId,
                RefreshToken = accessToken,
                ExpiresAt = tokenExpiryTime,
                IpAddress = "Check-Controller",
                UserAgent = "Web-Browser",
                CreatedAt = DateTime.UtcNow,
                RevokedAt = null
            };

            // 6. Lưu vào DB
            await _accountRepository.AddSessionAsync(newSession);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            // 7. Trả về kết quả
            var response = new LoginResponse
            {
                Token = accessToken,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl ?? "default-avatar"
            };

            return OperationResult<LoginResponse>.Success(response, 200, "Đăng nhập thành công!");
        }
    }
}