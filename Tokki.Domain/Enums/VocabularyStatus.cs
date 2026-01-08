using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum VocabularyStatus
    {
        [Description("Soạn thảo, Không hoạt động")]
        Draft = 0,
        Active = 1,
        Deleted = 2,
        [Description("Chờ phê duyệt")]
        PendingApproval = 3,
        [Description("Bị từ chối phê duyệt")]
        Rejected = 4
    }
}
