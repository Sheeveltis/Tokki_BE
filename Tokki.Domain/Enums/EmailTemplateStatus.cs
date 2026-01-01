using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum EmailTemplateStatus
    {
        [Description("Soạn thảo, Không hoạt động")]
        Draft = 0,

        [Description("Đang hoạt động")]
        Active = 1,

        [Description("Đã xóa")]
        Deleted = 2
    }
}
