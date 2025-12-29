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
        [Description("Soạn thảo, Không hoạt động")]
        Draft = 0,
        [Description("Đang hoạt động")]

        Active = 1,
        [Description("Đã xóa")]

        Deleted = 2
    }
}
