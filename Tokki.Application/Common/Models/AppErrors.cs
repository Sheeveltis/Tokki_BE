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
        public static readonly Error BlogNotFound = new("Blog.NotFound", "Bài viết không tìm thấy.");
        public static readonly Error CannotDeleteOthersBlog = new("Blog.UnauthorizedDelete", "Bạn không được xóa bài của người khác.");
        // --- NHÓM 4: CATEGORY ---
        public static readonly Error CategoryNotFound = new("Category.NotFound", "Danh mục yêu cầu không tồn tại.");
    }
}
