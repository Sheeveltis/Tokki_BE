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
        Draft = 1,

        [Description("Đã xuất bản")]
        Published = 2,

        [Description("Đã xóa")]
        Deleted = 3
    }
}
