using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum ExamStatsSortBy
    {
        [Description("Ngày tạo")]
        CreatedAt = 0,

        [Description("Số người tham gia")]
        Participants = 1,

        [Description("Lượt tải PDF")]
        PdfDownload = 2,

        [Description("Điểm trung bình")]
        AverageScore = 3
    }
}
