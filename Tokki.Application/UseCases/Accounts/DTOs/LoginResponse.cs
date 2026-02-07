namespace Tokki.Application.UseCases.Accounts.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; } 
    }
}
