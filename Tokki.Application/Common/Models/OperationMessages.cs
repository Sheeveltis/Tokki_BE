using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.Common.Models
{
    public static class OperationMessages
    {
        public static string CreateSuccess(string target) =>
            $"Đã thêm {target} thành công.";

        public static string CreateFail(string target) =>
            $"Không thể thêm {target}. Vui lòng thử lại.";

        public static string UpdateSuccess(string target) =>
            $"Đã cập nhật {target} thành công.";

        public static string UpdateFail(string target) =>
            $"Không thể cập nhật {target}. Vui lòng kiểm tra lại.";

        public static string DeleteSuccess(string target) =>
            $"Đã xóa {target} thành công.";

        public static string DeleteFail(string target) =>
            $"Không thể xóa {target}. Vui lòng thử lại hoặc liên hệ hỗ trợ.";

        public static string GetSuccess(string target) =>
            $"Đã lấy thông tin {target} thành công.";

        public static string GetFail(string target) =>
            $"Không thể lấy thông tin {target}. Vui lòng thử lại sau.";

        public static string NotFound(string target) =>
            $"{target} không tồn tại hoặc đã bị xóa.";

        public static string InvalidInput(string target) =>
            $"Thông tin {target} không hợp lệ. Vui lòng kiểm tra lại.";

        public static string AlreadyExists(string target) =>
            $"{target} đã tồn tại trong hệ thống.";

        public static string PaymentSuccess() =>
            "Thanh toán thành công. Dịch vụ đã được kích hoạt.";

        public static string PaymentPending() =>
            "Giao dịch đang chờ xử lý.";

        public static string InsufficientAmount(decimal received, decimal required) =>
            $"Số tiền chuyển ({received:N0}) thấp hơn giá trị gói ({required:N0}).";
    }
}
