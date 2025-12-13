
using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum UserTargetGroup {
        [Description("Gửi cho toàn bộ người dùng")]
        All = 0,
        [Description("Gửi cho người dùng miễn phí")]
        FreeUsers = 1,
        [Description("Gửi cho người dùng đang trả phí")]
        VipUsers = 2 }
}
