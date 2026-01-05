using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum TemplatePartStatus
    {
        [Description("Đang hoạt động")]
        Active = 1,

        [Description("Đã xóa")]
        Deleted = 0
    }
}