namespace Tokki.Application.Common.Helpers
{
    public static class LevelEngine
    {
        private const int BASE_XP = 100;
        private const double EXPONENT = 1.5; 

        public static int GetLevel(long totalXp)
        {
            if (totalXp < BASE_XP) return 1;
            return (int)Math.Floor(Math.Pow((double)totalXp / BASE_XP, 1.0 / EXPONENT)) + 1;
        }

        public static long GetTotalXpRequiredForLevel(int level)
        {
            if (level <= 1) return 0;
            return (long)(BASE_XP * Math.Pow(level - 1, EXPONENT));
        }
    }
}