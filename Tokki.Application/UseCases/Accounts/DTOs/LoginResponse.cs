namespace Tokki.Application.UseCases.Accounts.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; } 

        public bool RequireFacebookRegister { get; set; } = false;
        public string FacebookId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Birthday { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
    }
}
