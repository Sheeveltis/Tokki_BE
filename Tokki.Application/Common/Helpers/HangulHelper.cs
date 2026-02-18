using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.Common.Helpers
{
    public static class HangulHelper
    {
        private static readonly char[] InitialConsonants =
        {
            'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };

        private static readonly char[] Vowels =
        {
            'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ',
            'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ'
        };

        private static readonly char[] FinalConsonants =
        {
            '\0', // 0: Không có patchim
            'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ', 'ㄻ',
            'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ', 'ㅆ',
            'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };

        public class HangulParts
        {
            public char Initial { get; set; }
            public char Vowel { get; set; }
            public char Final { get; set; } // '\0' nếu không có patchim
        }

        /// <summary>
        /// Tách một ký tự Hangul thành 3 phần (Jamo)
        /// </summary>
        /// <param name="c">Ký tự cần tách (VD: '글')</param>
        /// <returns>Object chứa 3 phần tử (VD: ㄱ, ㅡ, ㄹ)</returns>
        public static HangulParts Decompose(char c)
        {
            if (c < 0xAC00 || c > 0xD7A3)
            {
                return new HangulParts { Initial = c, Vowel = ' ', Final = '\0' };
            }

            int baseCode = c - 0xAC00;

            int initialIndex = baseCode / (21 * 28);           
            int vowelIndex = (baseCode % (21 * 28)) / 28;      
            int finalIndex = baseCode % 28;                   

            return new HangulParts
            {
                Initial = InitialConsonants[initialIndex],
                Vowel = Vowels[vowelIndex],
                Final = FinalConsonants[finalIndex]
            };
        }
    }
}
