using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum EmailTemplateType
    {
        [Description("Nhắc nhở học (Offline X ngày)")]
        OfflineReminder = 1,

        [Description("Thông báo sắp hết hạn VIP (Còn X ngày)")]
        VipExpiringReminder = 2
    }
}
