using System.Security.Cryptography;
using System.Text;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Infrastructure.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _repo;

        public RefreshTokenService(IRefreshTokenRepository repo)
        {
            _repo = repo;
        }

        // ─── HASH ───────────────────────────────────────────────
        private static string HashToken(string raw)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        // ─── TẠO REFRESH TOKEN MỚI ──────────────────────────────
        public async Task<string> CreateRefreshTokenAsync(Account user)
        {
            var raw = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // 64 ký tự
            var hash = HashToken(raw);

            var token = new RefreshToken
            {
                TokenHash = hash,
                UserId = user.UserId,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                Revoked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(token);
            await _repo.SaveChangesAsync();

            return raw; // Trả raw, KHÔNG trả hash
        }

        // ─── XÁC MINH REFRESH TOKEN ─────────────────────────────
        public async Task<RefreshToken> VerifyRefreshTokenAsync(string rawToken)
        {
            var hash = HashToken(rawToken);
            var token = await _repo.GetByTokenHashAsync(hash)
                ?? throw new UnauthorizedAccessException("Invalid refresh token");

            if (token.Revoked)
                throw new UnauthorizedAccessException("Refresh token has been revoked");

            if (token.ExpiryDate < DateTime.UtcNow)
            {
                token.Revoked = true;
                await _repo.SaveChangesAsync();
                throw new UnauthorizedAccessException("Refresh token has expired");
            }

            return token;
        }

        // ─── ROTATE: thu hồi cũ, cấp mới ───────────────────────
        public async Task<string> RotateRefreshTokenAsync(RefreshToken old)
        {
            old.Revoked = true;
            await _repo.SaveChangesAsync();

            // Tạo token mới cho cùng user
            var newRaw = await CreateRefreshTokenAsync(old.User);
            return newRaw;
        }

        // ─── REVOKE 1 TOKEN ─────────────────────────────────────
        public async Task RevokeRefreshTokenAsync(string rawToken)
        {
            var hash = HashToken(rawToken);
            var token = await _repo.GetByTokenHashAsync(hash);

            if (token != null && !token.Revoked)
            {
                token.Revoked = true;
                await _repo.SaveChangesAsync();
            }
        }

        // ─── REVOKE TẤT CẢ TOKEN CỦA 1 USER ────────────────────
        public async Task RevokeAllRefreshTokensAsync(string userId)
        {
            var tokens = await _repo.GetAllByUserIdAsync(userId);

            foreach (var token in tokens.Where(t => !t.Revoked))
            {
                token.Revoked = true;
            }

            await _repo.SaveChangesAsync();
        }
    }
}