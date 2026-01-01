using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum GameResultStatus
    {
        [Description("Hoàn thành")]
        Completed = 0,

    [Description("Hết thời gian")]
        Timeout = 1,

    [Description("Người chơi thoát giữa chừng")]
        Quit = 2,

    [Description("Trận bị hủy, không tính kết quả")]
        Aborted = 3,

    [Description("Lỗi hệ thống hoặc mất kết nối")]
        Error = 4
    }
}
