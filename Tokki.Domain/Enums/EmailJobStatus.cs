using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum EmailJobStatus
    {
        [Description("Đang chờ xử lý")]
        Pending = 0,

        [Description("Đang tiến hành gửi")]
        Processing = 1,

        [Description("Đã gửi thành công")]
        Sent = 2,

        [Description("Gửi thất bại")]
        Failed = 3
    }
}