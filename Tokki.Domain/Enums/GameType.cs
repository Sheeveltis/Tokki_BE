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

        [Description(" Solitaire")]
        Solitaire = 2,
        [Description("Typing practice")]
        TypingPractice= 2,
    }
}
