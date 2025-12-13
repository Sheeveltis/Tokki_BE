using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum ExamType
    {
        [Description("TOPIK I")]
        TopikI = 1,

        [Description("TOPIK II")]
        TopikII = 2
    }
}
