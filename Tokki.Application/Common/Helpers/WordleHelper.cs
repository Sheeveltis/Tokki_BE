using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.Common.Helpers
{
    public static class WordleHelper
    {
        public static List<BlockFeedback> CalculateFeedback(string target, string guess)
        {
            var result = new List<BlockFeedback>();
            target = target.Normalize(NormalizationForm.FormC);
            guess = guess.Normalize(NormalizationForm.FormC);

            for (int i = 0; i < target.Length; i++)
            {
                var fb = new BlockFeedback { Character = guess[i] };

                var tParts = HangulHelper.Decompose(target[i]);
                var gParts = HangulHelper.Decompose(guess[i]);

                var tInitials = HangulHelper.GetSubJamos(tParts.Initial);
                var gInitials = HangulHelper.GetSubJamos(gParts.Initial);

                var tVowels = HangulHelper.GetSubJamos(tParts.Vowel);
                var gVowels = HangulHelper.GetSubJamos(gParts.Vowel);

                var tFinals = HangulHelper.GetSubJamos(tParts.Final);
                var gFinals = HangulHelper.GetSubJamos(gParts.Final);

                fb.InitialStatus = (tParts.Initial == gParts.Initial) ? "Green" :
                                   (tInitials.Intersect(gInitials).Any() ? "Yellow" : "Gray");

                fb.VowelStatus = (tParts.Vowel == gParts.Vowel) ? "Green" :
                                 (tVowels.Intersect(gVowels).Any() ? "Yellow" : "Gray");

                fb.FinalStatus = (tParts.Final == gParts.Final) ? "Green" :
                                 (tParts.Final != '\0' && gParts.Final != '\0' && tFinals.Intersect(gFinals).Any() ? "Yellow" : "Gray");

                if (target[i] == guess[i])
                {
                    fb.BlockColor = "Green";
                }
                else if (fb.InitialStatus == "Green" || fb.VowelStatus == "Green" || fb.FinalStatus == "Green" ||
                         fb.InitialStatus == "Yellow" || fb.VowelStatus == "Yellow" || fb.FinalStatus == "Yellow")
                {
                    fb.BlockColor = "Yellow";
                }
                else
                {
                    fb.BlockColor = "Gray";
                }

                result.Add(fb);
            }
            return result;
        }
    }
}
