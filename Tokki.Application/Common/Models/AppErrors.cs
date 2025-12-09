using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace Tokki.Application.Common.Models
{
    public static class AppErrors
    {
        // --- NHÓM 1: COMMON (Lỗi chung chung hệ thống) ---
        public static readonly Error ServerError = new("App.ServerError", "Lỗi hệ thống, vui lòng thử lại sau.");
        public static readonly Error ValidationFailed = new("App.ValidationFailed", "Dữ liệu đầu vào không hợp lệ.");
        // --- NHÓM 2: AUTHENTICATION (Đăng nhập/Đăng ký) ---
        public static readonly Error WrongPassword = new("Auth.WrongPassword", "Sai mật khẩu.");
        public static readonly Error UserNotFound = new("Auth.UserNotFound", "Tài khoản không tồn tại.");
        public static readonly Error EmailDuplicated = new("Auth.EmailDuplicated", "Email này đã được sử dụng.");

        // --- NHÓM 3: BLOG/POST ---
        public static readonly Error BlogNotFound = new("Blog.NotFound", OperationMessages.NotFound("Blog"));
        public static readonly Error CannotDeleteOthersBlog = new("Blog.UnauthorizedDelete", "Bạn không được xóa bài của người khác.");
        // --- NHÓM 4: CATEGORY ---
        public static readonly Error CategoryNotFound = new("Category.NotFound", OperationMessages.NotFound("Category"));

        //Paymets & VIP
        public static readonly Error VipPackageNotFound = new("VipPackage.NotFound", "Gói dịch vụ VIP không tồn tại.");
        public static readonly Error VipPackageInactive = new("VipPackage.Inactive", "Gói dịch vụ này hiện đang tạm ngừng kinh doanh.");
        public static readonly Error PaymentNotFound = new("Payment.NotFound", OperationMessages.NotFound("Giao dịch thanh toán"));
        public static readonly Error PaymentFailed = new("Payment.Failed", "Khởi tạo giao dịch thất bại.");
        public static readonly Error PaymentInvalidContent = new("Payment.InvalidContent", "Nội dung chuyển khoản không chứa mã đơn hàng hợp lệ.");
        public static readonly Error PaymentInsufficientAmount = new("Payment.InsufficientAmount", "Số tiền chuyển khoản không đủ để kích hoạt gói.");
        public static readonly Error PaymentAlreadyProcessed = new("Payment.AlreadyProcessed", "Giao dịch này đã được xử lý trước đó.");

        //Report
        public static readonly Error ReportNotFound = new("Report.NotFound", "Không tìm thấy báo cáo hoặc báo cáo đã bị xóa.");
        public static readonly Error ReportUnauthorized = new("Report.Unauthorized", "Bạn không có quyền thực hiện thao tác trên báo cáo này.");
        public static readonly Error ReportCannotDelete = new("Report.CannotDelete", "Không thể xóa báo cáo đang xử lý hoặc đã hoàn tất.");
        public static readonly Error ReportFetchFailed = new("Report.FetchFailed", "Đã xảy ra lỗi khi lấy danh sách báo cáo.");
        public static readonly Error ReportCreationFailed = new("Report.CreationFailed", "Không thể tạo báo cáo. Vui lòng thử lại.");
    }
}

