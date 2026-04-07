using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.IServices;

namespace Tokki.Application.Common.Helpers
{
    public class EmailNotificationHelper
    {
        private readonly IEmailService _emailService;

        public EmailNotificationHelper(IEmailService emailService)
        {
            _emailService = emailService;
        }

        /// <summary>
        /// Kho - Mẫu gửi mail Phê duyệt (Approve)
        /// </summary>
        /// <param name="toEmail"></param>
        /// <param name="fullName"></param>
        /// <param name="contentTitle"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public async Task SendContentApprovedAsync(string toEmail, string fullName, string contentTitle, string contentType)
        {
            var subject = $"[Tokki] {contentType} của bạn đã được phê duyệt";

            var bodyContent = $@"
                <p>
                    Chúc mừng bạn! {contentType} <strong>""{contentTitle}""</strong> của bạn 
                    đã được <strong>phê duyệt</strong> và hiện đã được công khai trên hệ thống Tokki.
                </p>
                <div style='background-color: #d4edda; color: #155724; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <strong>Trạng thái: Đã đăng (Published)</strong>
                </div>
                <p>Hãy truy cập hệ thống để kiểm tra ngay nhé.</p>";

            var fullHtml = BuildHtmlTemplate(fullName, bodyContent);
            await _emailService.SendEmailAsync(toEmail, subject, fullHtml);
        }

        /// <summary>
        /// Kho - Mẫu gửi mail Từ chối (Reject)
        /// </summary>
        /// <param name="toEmail"></param>
        /// <param name="fullName"></param>
        /// <param name="contentTitle"></param>
        /// <param name="contentType"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public async Task SendContentRejectedAsync(string toEmail, string fullName, string contentTitle, string contentType, string reason)
        {
            var subject = $"[Tokki] {contentType} của bạn chưa được phê duyệt";

            var bodyContent = $@"
                <p>
                    {contentType} <strong>""{contentTitle}""</strong> của bạn đã được xem xét nhưng 
                    <strong>chưa được phê duyệt</strong>.
                </p>
                <div style='background-color: #f8d7da; color: #721c24; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <p><strong>Lý do từ chối:</strong></p>
                    <p>{reason}</p>
                </div>
                <p>Vui lòng chỉnh sửa và gửi lại yêu cầu.</p>";

            var fullHtml = BuildHtmlTemplate(fullName, bodyContent);
            await _emailService.SendEmailAsync(toEmail, subject, fullHtml);
        }
 
        /// <summary>
        /// Mẫu gửi mail từ chối tự động bởi A.I
        /// </summary>
        public async Task SendBlogAIRejectedAsync(string toEmail, string fullName, string contentTitle, string reason, string adminEmail)
        {
            var subject = $"[Tokki AI] Thông báo về nội dung bài viết: {contentTitle}";
 
            var bodyContent = $@"
                <p>Hệ thống kiểm duyệt tự động của Tokki nhận thấy bài viết <strong>""{contentTitle}""</strong> của bạn có chứa nội dung chưa phù hợp.</p>
                
                <div style='background-color: #fff3cd; border: 1px solid #ffeeba; color: #856404; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <p><strong>Kết quả từ A.I:</strong> Nội dung không vượt qua bộ lọc tự động.</p>
                    <p><strong>Chi tiết:</strong> {reason}</p>
                </div>
 
                <p style='color: #721c24; font-weight: bold;'>Lưu ý: Đây là quyết định tự động từ hệ thống trí tuệ nhân tạo (A.I).</p>
                
                <p>Nếu bạn cho rằng có sự nhầm lẫn hoặc cần hỗ trợ thêm, vui lòng liên hệ trực tiếp với chúng tôi qua email: 
                   <a href='mailto:{adminEmail}' style='color: #007bff; text-decoration: none; font-weight: bold;'>{adminEmail}</a>
                </p>
                
                <p>Trân trọng,<br/>Đội ngũ Tokki</p>";
 
            var fullHtml = BuildHtmlTemplate(fullName, bodyContent);
            await _emailService.SendEmailAsync(toEmail, subject, fullHtml);
        }

        /// <summary>
        /// Gửi thông báo cho Admin khi hệ thống AI bị lỗi
        /// </summary>
        public async Task SendAIServiceFailureToAdminAsync(string adminEmail, string blogTitle, string errorCode)
        {
            var subject = $"[SYSTEM ALERT] AI Moderation Service Failed - {blogTitle}";
 
            var bodyContent = $@"
                <p>Hệ thống AI vừa gặp lỗi khi đang kiểm duyệt bài viết: <strong>""{blogTitle}""</strong>.</p>
                <div style='background-color: #f8d7da; color: #721c24; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <p><strong>Mã lỗi hệ thống:</strong> {errorCode}</p>
                    <p><strong>Trạng thái:</strong> Bài viết đã được chuyển sang hàng đợi duyệt thủ công.</p>
                </div>
                <p>Vui lòng kiểm tra lại cấu hình API hoặc hạn mức (quota) của Gemini.</p>";
 
            var fullHtml = BuildHtmlTemplate("Administrator", bodyContent);
            await _emailService.SendEmailAsync(adminEmail, subject, fullHtml);
        }
        /// <param name="fullName"></param>
        /// <param name="bodyContent"></param>
        /// <returns></returns>
        private string BuildHtmlTemplate(string fullName, string bodyContent)
        {
            var safeName = string.IsNullOrWhiteSpace(fullName) ? "Bạn" : fullName;
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                    <h2 style='color: #007bff; border-bottom: 1px solid #eee; padding-bottom: 10px;'>Tokki Notification</h2>
                    <div style='padding: 20px 0;'>
                        <h3>Xin chào {safeName},</h3>
                        {bodyContent}
                    </div>
                    <div style='font-size: 12px; color: #777; border-top: 1px solid #eee; padding-top: 10px;'>
                        Email tự động từ hệ thống Tokki.
                    </div>
                </div>";
        }
    }
}
