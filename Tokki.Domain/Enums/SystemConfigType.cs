using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum SystemConfigType
    {
        [Description("Cấu hình chung hệ thống")]
        General = 0,

        [Description("Cấu hình Trí tuệ nhân tạo (AI/Gemini)")]
        AI = 1,

        [Description("Cấu hình Mẫu thông báo (Notification Templates)")]
        Notification = 2,

        [Description("Cấu hình Bài viết và Blog")]
        Blog = 3,

        [Description("Cấu hình Game và Hệ thống XP (Gamification)")]
        Gamification = 4,

        [Description("Cấu hình Bảo mật và Xác thực (Security/Auth)")]
        Security = 5,

        [Description("Cấu hình Đề thi và Thi thử (Exam)")]
        Exam = 6,

        [Description("Cấu hình Từ vựng (Vocabulary)")]
        Vocabulary = 7,

        [Description("Cấu hình Lộ trình học tập (Roadmap)")]
        RoadMap = 8,

        [Description("Cấu hình Thanh toán và Gói cước (Payment/VIP)")]
        Payment = 9,

        [Description("Cấu hình Nội bộ hệ thống (Internal System)")]
        SystemInternal = 10,

        [Description("Cấu hình Giao diện người dùng (UI/Frontend)")]
        UI_Frontend = 11,

        [Description("Cấu hình Dịch vụ Email (SMTP/Mailing)")]
        EmailService = 12,

        [Description("Cấu hình Giọng đọc và Phát âm (Speech/Audio)")]
        AudioPronunciation = 13,

        [Description("Cấu hình Prompt cho AI")]
        AI_Prompt = 14
    }
}
