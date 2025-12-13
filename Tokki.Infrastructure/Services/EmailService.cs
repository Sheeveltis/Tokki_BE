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
        public async Task SendAccountInfoAsync(string toEmail, string fullName, string username, string password)
        {
            string subject = "[Tokki System] Thông tin tài khoản nhân viên mới";

            // Tạo nội dung HTML đẹp mắt
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Xin chào {fullName},</h2>
                    <p>Chào mừng bạn gia nhập đội ngũ <strong>Tokki</strong>.</p>
                    <p>Tài khoản truy cập hệ thống của bạn đã được khởi tạo thành công. Dưới đây là thông tin đăng nhập:</p>
                    
                    <div style='background-color: #f4f4f4; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p><strong>Email đăng nhập:</strong> {username}</p>
                        <p><strong>Mật khẩu mặc định:</strong> <span style='color: #d9534f; font-weight: bold;'>{password}</span></p>
                    </div>

                    <p style='color: red;'>⚠️ <strong>Lưu ý:</strong> Để bảo mật, vui lòng đăng nhập và đổi mật khẩu ngay trong lần truy cập đầu tiên.</p>
                    
                    <hr />
                    <p style='font-size: 12px; color: gray;'>Đây là email tự động, vui lòng không trả lời email này.</p>
                </div>
            ";

            // Gọi lại hàm gửi mail cơ bản
            await SendEmailAsync(toEmail, subject, body);
        }
    }
}