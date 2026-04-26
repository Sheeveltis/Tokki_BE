namespace Tokki.Application.UseCases.Gamification.DTOs
{
    public class StreakStatusDto
    {
        /// <summary>
        /// Chuỗi ngày liên tiếp hiện tại người dùng đạt mục tiêu học tập.
        /// </summary>
        public int CurrentStreak { get; set; }

        /// <summary>
        /// Chuỗi ngày dài nhất người dùng từng đạt được trong toàn bộ quá trình.
        /// </summary>
        public int MaxStreak { get; set; }

        /// <summary>
        /// Cờ đánh dấu hôm nay đã đạt được mục tiêu streak chưa (đủ 15 phút).
        /// </summary>
        public bool IsCompletedToday { get; set; }

        /// <summary>
        /// Số giây học tập tích lũy được trong ngày hôm nay.
        /// </summary>
        public double DailyStudySeconds { get; set; }

        /// <summary>
        /// Mục tiêu số giây cần đạt được để nhận streak (Mặc định 900s = 15p).
        /// </summary>
        public int TargetSeconds { get; set; }

        /// <summary>
        /// Phần trăm tiến độ học tập trong ngày so với mục tiêu.
        /// </summary>
        public double ProgressPercentage => TargetSeconds == 0 ? 0 : Math.Min(100, Math.Round((DailyStudySeconds / TargetSeconds) * 100, 2));
    }
}
