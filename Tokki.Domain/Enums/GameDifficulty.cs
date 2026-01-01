using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum GameDifficulty
    {
        
        
            [Description("Dễ")]
        Easy= 1,

            [Description("Bình thường")]
        Medium = 2,
        [Description("Khó")]
        Hard = 3

    }
}
