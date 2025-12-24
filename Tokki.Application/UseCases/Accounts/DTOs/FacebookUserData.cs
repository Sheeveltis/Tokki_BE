
namespace Tokki.Application.UseCases.Accounts.DTOs
{
    public class FacebookUserData
    {
        public string Id { get; set; } = string.Empty;

        // Email có thể không có -> nên để nullable để deserialize đúng
        public string? Email { get; set; }

        public string? Name { get; set; }

        public string? Birthday { get; set; }
        public string? Gender { get; set; }
    }
}
