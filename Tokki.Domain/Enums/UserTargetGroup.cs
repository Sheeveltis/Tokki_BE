using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum UserTargetGroup {
        [Description("Gửi cho toàn bộ người dùng")]
        All = 0,
        [Description("Gửi cho người dùng miễn phí")]
        FreeUsers = 1,
        [Description("Gửi cho người dùng đang trả phí")]
        VipUsers = 2 }
}
