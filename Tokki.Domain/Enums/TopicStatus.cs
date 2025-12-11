using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum TopicStatus
    {
        [Description("Bản nháp")]
        Draft = 0,

        [Description("Đang hoạt động")]
        Active = 1,

        [Description("Ngưng hoạt động")]
        Inactive = 2,

        [Description("Đã xóa")]
        Deleted = 3
    }
}
