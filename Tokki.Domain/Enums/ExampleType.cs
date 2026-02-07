using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum ExampleType
    {
        [Description("Không có ví dụ")]
        None = 0,

        [Description("Hội thoại")]
        Conversation = 1,

        [Description("Đoạn văn")]
        Passage = 2,

        [Description("Hình ảnh")]
        Image = 3
    }
}
