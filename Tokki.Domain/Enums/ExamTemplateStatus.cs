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
        Deleted = 99
    }
}
