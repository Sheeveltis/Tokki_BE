using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Tokki.Application.Common.Helpers; // Nơi bạn để JwtSettings
using Tokki.Application.IServices;      // Nơi bạn để Interface
using Tokki.Domain.Entities;

namespace Tokki.Infrastructure.Services // Đặt chung namespace với Services hiện có
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenGenerator(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;
        }

        public string GenerateToken(Account user, DateTime expiresAt) // <--- 1. Thêm tham số nhận giờ từ bên ngoài
        {
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("role", user.Role.ToString()) 
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAt, 
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public string GenerateForgotPasswordToken(string email)
        {
            var claims = new List<Claim>
    {
        // ✅ Chỉ cần 3 claims này thôi
        new Claim(ClaimTypes.Email, email),           // Claim email chuẩn
        new Claim(JwtRegisteredClaimNames.Sub, email), // Sub = email (để backup)
        new Claim("token_type", "reset_password")      // Đánh dấu loại token
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15), // Token ngắn hạn 15 phút
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}