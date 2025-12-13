
using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum PassageStatus
    {
        [Description("Đang hoạt động")]
        Active = 1,

        [Description("Đã ẩn")]
        Hidden = 2
    }
}
