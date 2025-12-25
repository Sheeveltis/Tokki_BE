namespace Tokki.Application.UseCases.Accounts.DTOs
{
    public class FacebookLoginResponse
    {
        // Login OK => có Token
        // Require register => Token rỗng
        public string Token { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;   // gộp Name vào đây
        public string Role { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        public bool RequireFacebookRegister { get; set; } = false;

        // Chỉ dùng khi RequireFacebookRegister = true
        public string FacebookId { get; set; } = string.Empty;
    }
}
