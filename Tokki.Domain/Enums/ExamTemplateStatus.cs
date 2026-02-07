using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum ExamTemplateStatus
    {
        [Description("Nháp")]
        Draft = 0,

        [Description("Đã xuất bản")]
        Published = 1,

        [Description("Đã xóa")]
        Deleted = 2,

        [Description("Chờ phê duyệt")]
        PendingApproval = 3,

        [Description("Từ chối")]
        Rejected = 4
    }
}
