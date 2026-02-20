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
        public static HashSet<char> GetSubJamos(char jamo)
        {
            return jamo switch
            {
                'ㅘ' => new HashSet<char> { 'ㅗ', 'ㅏ' },
                'ㅙ' => new HashSet<char> { 'ㅗ', 'ㅐ' },
                'ㅚ' => new HashSet<char> { 'ㅗ', 'ㅣ' },
                'ㅝ' => new HashSet<char> { 'ㅜ', 'ㅓ' },
                'ㅞ' => new HashSet<char> { 'ㅜ', 'ㅔ' },
                'ㅟ' => new HashSet<char> { 'ㅜ', 'ㅣ' },
                'ㅢ' => new HashSet<char> { 'ㅡ', 'ㅣ' },

                'ㄲ' => new HashSet<char> { 'ㄱ', 'ㄱ' },
                'ㄳ' => new HashSet<char> { 'ㄱ', 'ㅅ' },
                'ㄵ' => new HashSet<char> { 'ㄴ', 'ㅈ' },
                'ㄶ' => new HashSet<char> { 'ㄴ', 'ㅎ' },
                'ㄺ' => new HashSet<char> { 'ㄹ', 'ㄱ' },
                'ㄻ' => new HashSet<char> { 'ㄹ', 'ㅁ' },
                'ㄼ' => new HashSet<char> { 'ㄹ', 'ㅂ' },
                'ㄽ' => new HashSet<char> { 'ㄹ', 'ㅅ' },
                'ㄾ' => new HashSet<char> { 'ㄹ', 'ㅌ' },
                'ㄿ' => new HashSet<char> { 'ㄹ', 'ㅍ' },
                'ㅀ' => new HashSet<char> { 'ㄹ', 'ㅎ' },
                'ㅄ' => new HashSet<char> { 'ㅂ', 'ㅅ' },
                'ㅆ' => new HashSet<char> { 'ㅅ', 'ㅅ' },

                _ => new HashSet<char> { jamo }
            };
        }
    }
}
