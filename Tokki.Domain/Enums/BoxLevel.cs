using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum BoxLevel
    {
        [Description("Chưa học")]
        New = 0,       
        [Description("Mới học")]
        Learning = 1,   // Mới học - Cần ôn sau 1 ngày (Hũ 1)
        [Description("Đang nhớ")]
        Reviewing = 2,  // Đang nhớ - Cần ôn sau 3 ngày (Hũ 2)
        [Description("Nhớ tốt")]
        Mastering = 3,  // Nhớ tốt - Cần ôn sau 7 ngày (Hũ 3)
        [Description("Nhớ rất kỹ")]
        Advanced = 4,   // Nhớ rất kỹ - Cần ôn sau 2 tuần (Hũ 4)
        [Description("Thuộc lòng")]
        Mastered = 5

    }
}
