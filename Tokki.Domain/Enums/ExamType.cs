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
        EntranceTest = 3,

        [Description("Kiểm tra tuần")]
        WeeklyAssessment = 4

    }
}