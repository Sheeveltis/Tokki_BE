
using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum OtpStatus
    {
        [Description("Đã tạo, chưa sử dụng")]
        Active = 0,

        [Description("Đã sử dụng hợp lệ")]
        Used = 1,

        [Description("Đã bị thu hồi / vô hiệu hóa")]
        Revoked = 2,

        [Description("Đã hết hạn")]
        Expired = 3
    }

}
