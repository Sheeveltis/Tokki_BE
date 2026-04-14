using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum GameType
    {
        [Description("Matching card game")]
         MatchingCard= 1,

        [Description("Typing practice")] 
        Solitaire = 2,
        [Description("Solitaire")]
        TypingPractice= 3,
    }
}
