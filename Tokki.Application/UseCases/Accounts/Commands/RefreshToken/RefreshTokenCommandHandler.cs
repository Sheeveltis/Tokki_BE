// Application/UseCases/Accounts/Commands/RefreshToken/RefreshTokenCommandHandler.cs
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.DTOs;

namespace Tokki.Application.UseCases.Accounts.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, OperationResult<LoginResponse>>
    {
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IJwtTokenGenerator _jwtGenerator;

        public RefreshTokenCommandHandler(
            IRefreshTokenService refreshTokenService,
            IJwtTokenGenerator jwtGenerator)
        {
            _refreshTokenService = refreshTokenService;
            _jwtGenerator = jwtGenerator;
        }

        public async Task<OperationResult<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // 1. Xác minh refresh token cũ (throws nếu invalid/revoked/expired)
            var oldToken = await _refreshTokenService.VerifyRefreshTokenAsync(request.RawRefreshToken);

            // 2. Lấy user từ token
            var user = oldToken.User;

            // 3. Tạo access token mới
            var tokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(60);
            var newAccessToken = _jwtGenerator.GenerateToken(user, tokenExpiresAtUtc);

            // 4. Rotate refresh token (thu hồi cũ, cấp mới)
            var newRawRefreshToken = await _refreshTokenService.RotateRefreshTokenAsync(oldToken);

            var response = new LoginResponse
            {
                Token = newAccessToken,
                RefreshToken = newRawRefreshToken, // Controller sẽ set cookie rồi null field này
                FullName = user.FullName,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl ?? "default-avatar"
            };

            return OperationResult<LoginResponse>.Success(response, 200, "Làm mới token thành công!");
        }
    }
}