using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum ExamType
    {
        [Description("TOPIK I")]
        TopikI = 1,

        [Description("TOPIK II")]
        TopikII = 2,

        [Description("Test đầu vào")]
        [Obsolete("Dùng EntranceTestTopikI hoặc EntranceTestTopikII thay thế")]
        EntranceTest = 3,

        [Description("Kiểm tra tuần")]
        WeeklyAssessment = 4,

        [Description("Test đầu vào TOPIK I")]
        EntranceTestTopikI = 5,

        [Description("Test đầu vào TOPIK II")]
        EntranceTestTopikII = 6
    }
}