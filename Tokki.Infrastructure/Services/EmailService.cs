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

        public async Task SendFacebookAccountInfoAsync(string toEmail, string fullName, string email, string password)
        {
            var subject = "🎉 Chào mừng bạn đến với Tokki!";

            var body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 10px 10px 0 0; text-align: center;'>
                <h1 style='color: white; margin: 0; font-size: 28px;'>Chào mừng đến với Tokki! 🎊</h1>
            </div>
            
            <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
                <p style='font-size: 16px; color: #333; margin-bottom: 20px;'>
                    Xin chào <strong>{fullName}</strong>,
                </p>
                
                <p style='font-size: 16px; color: #333; line-height: 1.6;'>
                    Cảm ơn bạn đã đăng ký tài khoản thông qua Facebook! 🎉<br/>
                    Để bạn có thể đăng nhập bằng email khi cần, chúng tôi đã tạo tài khoản với thông tin sau:
                </p>
                
                <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea;'>
                    <p style='margin: 10px 0; font-size: 15px;'>
                        <strong style='color: #667eea;'>📧 Email:</strong> 
                        <span style='color: #333;'>{email}</span>
                    </p>
                    <p style='margin: 10px 0; font-size: 15px;'>
                        <strong style='color: #667eea;'>🔑 Mật khẩu:</strong> 
                        <code style='background: #f0f0f0; padding: 5px 10px; border-radius: 4px; color: #d63031; font-size: 16px;'>{password}</code>
                    </p>
                </div>
                
                <div style='background: #fff3cd; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                    <p style='margin: 0; font-size: 14px; color: #856404;'>
                        ⚠️ <strong>Lưu ý bảo mật:</strong> Vui lòng đổi mật khẩu ngay sau lần đăng nhập đầu tiên để bảo vệ tài khoản của bạn.
                    </p>
                </div>
                
                <p style='font-size: 16px; color: #333; line-height: 1.6; margin-top: 20px;'>
                    Bạn có thể đăng nhập bằng:
                </p>
                <ul style='font-size: 15px; color: #555; line-height: 1.8;'>
                    <li>Facebook (như đã đăng ký)</li>
                    <li>Email và mật khẩu được cung cấp ở trên</li>
                </ul>
                
                <div style='text-align: center; margin-top: 30px;'>
                    <a href='https://tokki.com/login' 
                       style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                              color: white; 
                              padding: 12px 30px; 
                              text-decoration: none; 
                              border-radius: 5px; 
                              font-weight: bold;
                              display: inline-block;'>
                        Đăng nhập ngay
                    </a>
                </div>
                
                <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'/>
                
                <p style='font-size: 14px; color: #777; text-align: center; margin-top: 20px;'>
                    Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email 
                    <a href='mailto:support@tokki.com' style='color: #667eea;'>support@tokki.com</a>
                </p>
                
                <p style='font-size: 14px; color: #777; text-align: center; margin-top: 10px;'>
                    Trân trọng,<br/>
                    <strong>Đội ngũ Tokki</strong>
                </p>
            </div>
        </div>
    ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendGoogleAccountInfoAsync(
     string toEmail,
     string toName,
     string usernameOrEmail,
     string defaultPassword)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("toEmail is required.", nameof(toEmail));

            // Đồng bộ style với Facebook nhưng bỏ icon
            var subject = "Chào mừng bạn đến với Tokki! (Google Login)";

            var safeName = string.IsNullOrWhiteSpace(toName) ? toEmail : toName;

            var body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background: linear-gradient(135deg, #4285F4 0%, #34A853 100%); padding: 30px; border-radius: 10px 10px 0 0; text-align: center;'>
                <h1 style='color: white; margin: 0; font-size: 24px;'>Chào mừng đến với Tokki</h1>
            </div>
            
            <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
                <p style='font-size: 16px; color: #333; margin-bottom: 20px;'>
                    Xin chào <strong>{safeName}</strong>,
                </p>

                <p style='font-size: 16px; color: #333; line-height: 1.6;'>
                    Bạn đã đăng ký/đăng nhập thành công thông qua Google.
                    Để bạn có thể đăng nhập bằng email khi cần, hệ thống đã tạo thông tin đăng nhập như sau:
                </p>

                <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #4285F4;'>
                    <p style='margin: 10px 0; font-size: 15px;'>
                        <strong style='color: #4285F4;'>Email:</strong> 
                        <span style='color: #333;'>{usernameOrEmail}</span>
                    </p>
                    <p style='margin: 10px 0; font-size: 15px;'>
                        <strong style='color: #4285F4;'>Mật khẩu mặc định:</strong> 
                        <code style='background: #f0f0f0; padding: 5px 10px; border-radius: 4px; color: #d63031; font-size: 16px;'>{defaultPassword}</code>
                    </p>
                </div>

                <div style='background: #fff3cd; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                    <p style='margin: 0; font-size: 14px; color: #856404;'>
                        Lưu ý bảo mật: Vui lòng đổi mật khẩu ngay sau lần đăng nhập đầu tiên để bảo vệ tài khoản của bạn.
                    </p>
                </div>

                <p style='font-size: 16px; color: #333; line-height: 1.6; margin-top: 20px;'>
                    Bạn có thể đăng nhập bằng:
                </p>
                <ul style='font-size: 15px; color: #555; line-height: 1.8;'>
                    <li>Google (như đã đăng ký)</li>
                    <li>Email và mật khẩu được cung cấp ở trên</li>
                </ul>

                <div style='text-align: center; margin-top: 30px;'>
                    <a href='https://tokki.com/login' 
                       style='background: linear-gradient(135deg, #4285F4 0%, #34A853 100%); 
                              color: white; 
                              padding: 12px 30px; 
                              text-decoration: none; 
                              border-radius: 5px; 
                              font-weight: bold;
                              display: inline-block;'>
                        Đăng nhập ngay
                    </a>
                </div>

                <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'/>

                <p style='font-size: 14px; color: #777; text-align: center; margin-top: 20px;'>
                    Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email 
                    <a href='mailto:support@tokki.com' style='color: #4285F4;'>support@tokki.com</a>
                </p>

                <p style='font-size: 14px; color: #777; text-align: center; margin-top: 10px;'>
                    Trân trọng,<br/>
                    <strong>Đội ngũ Tokki</strong>
                </p>

                <p style='font-size: 12px; color: gray; text-align: center; margin-top: 15px;'>
                    Đây là email tự động, vui lòng không trả lời email này.
                </p>
            </div>
        </div>
    ";

            await SendEmailAsync(toEmail, subject, body);
        }

    }
}