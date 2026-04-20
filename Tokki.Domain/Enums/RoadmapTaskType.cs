using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum RoadmapTaskType
    {
        [Description("Học lý thuyết")]
        LearnTheory = 0,   

        [Description("Quiz")]
        VirtualQuiz = 1, 
        [Description("Bài thi cuối tuần")]
        WeeklyExam = 2,
        [Description("Tài liệu ôn tập")]
        Document = 3
    }
}
