
using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum BlogStatus
    {
        [Description("Nháp")]
        Draft = 0,

        [Description("Đã đăng")]
        Published = 1,

        [Description("Đã ẩn")]
        Hidden = 2,
        //Lưu trữ (Kiểu Published thì sẽ ưu tiên hiện cho mn xem, còn Archived này thì nó không ưu tiên hiện, ai có link mới truy cập 
        //Ví dụ Lịch đăng ký thi TOPIK 2022 thì ko cần hiển thị lên cho mn, nhưng cũng ko nhất thiết phải xóa
        [Description("Lưu trữ")]
        Archived = 3,
        [Description("Chờ Admin duyệt")]
        PendingApproval = 4,
        [Description("Đã từ chối")]
        Rejected = 5,
        [Description("AI đang kiểm duyệt")]
        UnderAIReview = 6

    }
}
