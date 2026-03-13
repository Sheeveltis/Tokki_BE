using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum UserExamStatus
    {
        [Description("Đang làm")]
        InProgress = 0,
        [Description("Hoàn thành")]
        Completed = 1,
        [Description("Thoát giữa chừng")]
        Abandoned = 2  
    }
}
