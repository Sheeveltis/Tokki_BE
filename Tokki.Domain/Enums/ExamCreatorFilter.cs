using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum ExamCreatorFilter
    {
        [Description("Tất cả")]
        All = 0,

        [Description("Hệ thống A.I")]
        AI = 1,

        [Description("Người tạo")]
        Human = 2
    }
}
