using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum UserFavoriteWordStatus
    {
        [Description("Đang yêu thích")]
        Active = 1,

        [Description("Đã bỏ yêu thích")]
        Removed = 2
    }

}
