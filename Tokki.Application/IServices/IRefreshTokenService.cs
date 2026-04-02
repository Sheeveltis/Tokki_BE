using Tokki.Domain.Entities;

namespace Tokki.Application.IServices
{
    public interface IRefreshTokenService
    {
        Task<string> CreateRefreshTokenAsync(Account user);          
        Task<RefreshToken> VerifyRefreshTokenAsync(string rawToken);  
        Task<string> RotateRefreshTokenAsync(RefreshToken old);       
        Task RevokeRefreshTokenAsync(string rawToken);
        Task RevokeAllRefreshTokensAsync(string userId);
    }
}
