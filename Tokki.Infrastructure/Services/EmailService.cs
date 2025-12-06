using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;
using Tokki.Application.IServices;

namespace Tokki.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromEmail; // Sửa tên biến cho đúng nghĩa
        private readonly string _fromName;  // Thêm biến hứng tên hiển thị
        private readonly string _password;  // Sửa tên biến cho đúng nghĩa

        public EmailService(IConfiguration configuration)
        {
            var emailSettings = configuration.GetSection("EmailSettings");

            // Đọc đúng key từ JSON bạn cung cấp
            _smtpHost = emailSettings["SmtpHost"]!;
            _smtpPort = int.Parse(emailSettings["SmtpPort"]!);

            // KEY MỚI: Phải khớp chữ hoa chữ thường với JSON
            _fromEmail = emailSettings["FromEmail"]!;
            _fromName = emailSettings["FromName"]!;
            _password = emailSettings["Password"]!;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var mail = new MailMessage();

            // Sử dụng thông tin từ JSON
            mail.From = new MailAddress(_fromEmail, _fromName);
            mail.To.Add(new MailAddress(toEmail));
            mail.Subject = subject;
            mail.Body = message;
            mail.IsBodyHtml = true;

            using (var smtpClient = new SmtpClient(_smtpHost, _smtpPort))
            {
                // Sử dụng mật khẩu ứng dụng từ JSON
                smtpClient.Credentials = new NetworkCredential(_fromEmail, _password);
                smtpClient.EnableSsl = true;

                await smtpClient.SendMailAsync(mail);
            }
        }
    }
}