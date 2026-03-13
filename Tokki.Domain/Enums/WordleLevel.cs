using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum WordleLevel
    {
        [Description("Dễ")] //2 ký tự (block) nè
        Easy = 1,    
        [Description("Trung bình")] // 3 ký tự (block)
        Medium = 2,  
        [Description("Khó")] // 4 ký tự (block)
        Hard = 3     
    }
}
