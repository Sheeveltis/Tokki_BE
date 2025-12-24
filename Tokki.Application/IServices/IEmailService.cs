namespace Tokki.Application.IServices
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        Task SendAccountInfoAsync(string toEmail, string fullName, string username, string password);
        Task SendFacebookAccountInfoAsync(string toEmail, string fullName, string email, string password);

    }
}