using Tokki.Application.Common.Models; // Đảm bảo namespace chứa class Error

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
        public static readonly Error PhoneNumberDuplicated = new("Auth.PhoneNumberDuplicated", "Số điện thoại này đã được sử dụng.");
        public static readonly Error UserNotFoundById = new("User.NotFoundById", "Người dùng không tồn tại.");
        public static readonly Error UserUnauthorized = new("User.Unauthorized", "Không xác định được người dùng.");
        public static readonly Error CannotDeleteSelf = new("User.CannotDeleteSelf", "Bạn không thể xóa chính mình.");
        public static readonly Error UserInactive = new("User.Inactive", "Tài khoản người dùng đã bị vô hiệu hóa.");

        //  Lỗi khi tài khoản bị khóa vĩnh viễn (Banned)
        public static readonly Error AccountBanned = new("Auth.AccountBanned", "Tài khoản của bạn đã bị khóa vĩnh viễn.");

        //  Lỗi khi tài khoản bị tạm khóa (Locked)
        // Note: Message ở đây là mặc định, FE sẽ dựa vào Code "Auth.AccountLocked" để hiển thị thông báo chi tiết (vd: kèm thời gian).
        public static readonly Error AccountLocked = new("Auth.AccountLocked", "Tài khoản đang bị tạm khóa do đăng nhập sai nhiều lần.");

        // --- NHÓM 3: BLOG/POST ---
        public static readonly Error BlogNotFound = new("Blog.NotFound", "Bài viết không tìm thấy.");
        public static readonly Error CannotDeleteOthersBlog = new("Blog.UnauthorizedDelete", "Bạn không được xóa bài của người khác.");

        // --- NHÓM 4: CATEGORY ---
        public static readonly Error CategoryNotFound = new("Category.NotFound", "Danh mục yêu cầu không tồn tại.");

        public static readonly Error ConfigNotFound = new("Config.NotFound", "Không tìm thấy cấu hình.");
        public static readonly Error ConfigKeyDuplicated = new("Config.KeyDuplicated", "Key cấu hình đã tồn tại.");
        public static readonly Error ConfigKeyInvalid = new("Config.KeyInvalid", "Key cấu hình không hợp lệ.");
        public static readonly Error ConfigValueInvalid = new("Config.ValueInvalid", "Giá trị cấu hình không hợp lệ.");

        public static readonly Error OtpInvalid = new("Otp.Invalid", "OTP không hợp lệ hoặc đã hết hạn.");
        public static readonly Error OtpCodeWrong = new("Otp.CodeWrong", "Sai mã OTP.");
        public static readonly Error OtpExpired = new("Otp.Expired", "OTP đã hết hạn.");
        public static readonly Error OtpUsed = new("Otp.Used", "OTP đã được sử dụng.");
        public static readonly Error OtpNotFound = new("Otp.NotFound", "Mã xác thực không tồn tại hoặc đã hết hạn.");
        public static readonly Error OtpRateLimitExceeded = new("Otp.RateLimitExceeded", "Vui lòng đợi trước khi gửi lại OTP.");
        public static readonly Error OtpMaxRetryExceeded = new("Otp.MaxRetryExceeded", "Bạn đã nhập sai quá số lần quy định. Mã xác thực đã bị hủy.");
        public static readonly Error OtpRevoked = new("Otp.Revoked", "Mã xác thực đã bị khóa do nhập sai quá nhiều lần.");
        public static readonly Error EmailServiceError = new("Otp.EmailServiceError", "Hệ thống gửi mail đang gặp sự cố. Vui lòng thử lại sau.");

        public static readonly Error EmailTemplateNotFound = new("EmailTemplate.NotFound", "Không tìm thấy template email.");
        public static readonly Error EmailTemplateKeyDuplicated = new("EmailTemplate.KeyDuplicated", "TemplateKey đã tồn tại.");
        public static readonly Error EmailTemplateKeyInvalid = new("EmailTemplate.KeyInvalid", "TemplateKey không hợp lệ.");
        public static readonly Error EmailTemplateInUse = new("EmailTemplate.InUse", "Template đang được sử dụng, không thể xóa.");

        public static readonly Error EmailCampaignNotFound = new("EmailCampaign.NotFound", "Chiến dịch email không tồn tại.");
        public static readonly Error EmailCampaignInvalidSchedule = new("EmailCampaign.InvalidSchedule", "Thời gian lên lịch không hợp lệ.");
        public static readonly Error EmailCampaignAlreadySent = new("EmailCampaign.AlreadySent", "Chiến dịch đã được gửi.");
    }
}