using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum AccountStatus
    {
        // Nên để Inactive là 0 để lỡ quên set giá trị thì mặc định là Inactive (An toàn)
        [Description("Vô hiệu hóa")]
        Inactive = 0,

        [Description("Hoạt động")]
        Active = 1,

        [Description("Đã bị khóa")]
        Banned = 2
    }
}