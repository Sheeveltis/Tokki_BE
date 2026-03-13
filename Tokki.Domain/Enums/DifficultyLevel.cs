
using System.ComponentModel;


namespace Tokki.Domain.Enums
{
    public enum DifficultyLevel
    {
        [Description("Dễ")]
        Easy = 1,

        [Description("Trung bình")]
        Medium = 2,

        [Description("Khó")]
        Hard = 3,
        [Description("Rất khó")]
        VeryHard = 4

    }
}
