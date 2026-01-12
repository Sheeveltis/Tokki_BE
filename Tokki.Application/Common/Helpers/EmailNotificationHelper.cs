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
        /// Kho - Xây dựng mẫu HTML chung cho email
        /// </summary>
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
