using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum AccountRole
    {
        [Description("Người dùng")]
        User = 0,

        [Description("Quản trị viên")]
        Admin = 1,

        [Description("Nhân viên")]
        Staff = 2
    }
}
