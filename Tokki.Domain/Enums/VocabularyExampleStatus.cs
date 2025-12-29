using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum VocabularyExampleStatus
    {
        [Description("Đang hoạt động")]

        Active = 1,
        [Description("Đã xóa")]

        Deleted = 2,
        [Description("Không hoạt động")]
        Inactive = 3,
    }
}
