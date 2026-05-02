using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum SystemConfigType
    {
        [Description("Hệ thống & Giao diện (Chung, UI, Nội bộ)")]
        General = 0,

        [Description("Trí tuệ nhân tạo (AI & Prompts)")]
        AI = 1,

        [Description("Thông báo & Liên lạc (Push, Email, Chat)")]
        Communication = 2,

        [Description("Nội dung học tập (Vocabulary, Roadmap, Blog, Audio)")]
        Learning = 3,

        [Description("Game & XP (Gamification)")]
        Gamification = 4,

        [Description("Bảo mật & Xác thực")]
        Security = 5,

        [Description("Đề thi & Đánh giá (Assessment)")]
        Assessment = 6,

        [Description("Thanh toán & Gói cước (Billing/VIP)")]
        Billing = 7
    }
}
