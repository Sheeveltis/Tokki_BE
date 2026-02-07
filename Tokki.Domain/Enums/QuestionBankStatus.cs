using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum QuestionBankStatus
    {
        [Description("Soạn thảo, Không hoạt động")]
        Draft = 0,
        [Description("Đang hoạt động")]
        Active = 1,
        [Description("Đã xóa")]
        Deleted = 2,
        [Description("Chờ phê duyệt")]
        PendingApproval = 3,
        [Description("Bị từ chối phê duyệt")]
        Rejected = 4,
        [Description("Câu hỏi đã được sử dụng trong đề.")]
        Assigned = 5
    }
}
