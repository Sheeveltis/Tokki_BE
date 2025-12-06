using System.Reflection.Emit;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories; // ✅ Dùng Interface Repository
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.Login;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Queries.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, OperationResult<LoginResponse>>
    {
        // ❌ Bỏ TokkiDbContext
        // private readonly TokkiDbContext _context; 

        // ✅ Thay bằng IAccountRepository
        private readonly IAccountRepository _accountRepository;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IIdGeneratorService _idGenerator; 
        public LoginCommandHandler(IAccountRepository accountRepository, IJwtTokenGenerator jwtGenerator, IIdGeneratorService idGenerator)
        {
            _accountRepository = accountRepository;
            _jwtGenerator = jwtGenerator;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm User thông qua Repository
            var user = await _accountRepository.GetByEmailAsync(request.Email);

            if (user == null)
            {
                return OperationResult<LoginResponse>.Failure("Tài khoản hoặc mật khẩu không chính xác.", 400);
            }

            // 2. Kiểm tra mật khẩu bằng BCrypt
            // Lưu ý: Phải so sánh với PasswordHash trong DB
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

            // Thời gian hết hạn của Token (nên lấy từ Config, tạm thời để 2 tiếng)
            var tokenExpiryTime = DateTime.UtcNow.AddMinutes(1);

            // 5. Tạo Session Entity
            var newSession = new Session
            {
                // Tạo ID session (Dùng hàm Generate của bạn)
                SessionId = _idGenerator.Generate(10),

                UserId = user.UserId,

                // Lưu JWT vào cột RefreshToken (Theo logic bạn muốn lưu token để quản lý)
                // Lưu ý: Đúng chuẩn thì nên tạo 1 chuỗi ngẫu nhiên riêng làm RefreshToken, 
                // nhưng ở đây mình gán accessToken vào để code chạy được theo ý bạn.
                RefreshToken = accessToken,

                ExpiresAt = tokenExpiryTime,

                // Các thông tin Client (sau này lấy từ Controller)
                IpAddress = "Check-Controller",
                UserAgent = "Web-Browser",

                CreatedAt = DateTime.UtcNow,
                RevokedAt = null // Mặc định là null (chưa bị hủy)
            };

            // 6. Lưu vào DB (CHỈ GỌI 1 LẦN)
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