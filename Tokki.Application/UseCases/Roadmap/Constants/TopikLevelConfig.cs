using Tokki.Domain.Enums;

namespace Tokki.Application.Common.Constants
{
    public static class TopikLevelConfig
    {
        public static readonly Dictionary<TargetAimLevel, TopikLevelInfo> Levels = new()
        {
            {
                TargetAimLevel.Topik_I_Level1,
                new(
                    PassScore: 80,
                    TotalScore: 200,
                    ExamGroup: "TOPIK_I",
                    DisplayName: "TOPIK I - Level 1",
                    EntranceExamType: ExamType.EntranceTestTopikI,
                    ConfigKey: "ENTRANCE_EXAM_TOPIK_1"
                )
            },
            {
                TargetAimLevel.Topik_I_Level2,
                new(
                    PassScore: 140,
                    TotalScore: 200,
                    ExamGroup: "TOPIK_I",
                    DisplayName: "TOPIK I - Level 2",
                    EntranceExamType: ExamType.EntranceTestTopikI,
                    ConfigKey: "ENTRANCE_EXAM_TOPIK_2"
                )
            },
            {
                TargetAimLevel.Topik_II_Level3,
                new(
                    PassScore: 120,
                    TotalScore: 300,
                    ExamGroup: "TOPIK_II",
                    DisplayName: "TOPIK II - Level 3",
                    EntranceExamType: ExamType.EntranceTestTopikII,
                    ConfigKey: "ENTRANCE_EXAM_TOPIK_3"
                )
            },
            {
                TargetAimLevel.Topik_II_Level4,
                new(
                    PassScore: 150,
                    TotalScore: 300,
                    ExamGroup: "TOPIK_II",
                    DisplayName: "TOPIK II - Level 4",
                    EntranceExamType: ExamType.EntranceTestTopikII,
                    ConfigKey: "ENTRANCE_EXAM_TOPIK_4"
                )
            },
            {
                TargetAimLevel.Topik_II_Level5,
                new(
                    PassScore: 190,
                    TotalScore: 300,
                    ExamGroup: "TOPIK_II",
                    DisplayName: "TOPIK II - Level 5",
                    EntranceExamType: ExamType.EntranceTestTopikII,
                    ConfigKey: "ENTRANCE_EXAM_TOPIK_5"
                )
            },
            {
                TargetAimLevel.Topik_II_Level6,
                new(
                    PassScore: 230,
                    TotalScore: 300,
                    ExamGroup: "TOPIK_II",
                    DisplayName: "TOPIK II - Level 6",
                    EntranceExamType: ExamType.EntranceTestTopikII,
                    ConfigKey: "ENTRANCE_EXAM_TOPIK_6"
                )
            },
        };
    }
    public record TopikLevelInfo(
        int PassScore,
        int TotalScore,
        string ExamGroup,
        string DisplayName,
        ExamType EntranceExamType,
        string ConfigKey           
    );
}