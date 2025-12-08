using System.ComponentModel;

namespace Tokki.Domain.Enums
{
    public enum OtpType
    {
        // Dùng khi đăng ký tài khoản mới
        [Description("Xác thực Email")]
        VerifyEmail = 0,

        // Dùng khi người dùng bấm "Quên mật khẩu"
        [Description("Khôi phục mật khẩu")]
        ResetPassword = 1,

        // Dùng khi đăng nhập ở thiết bị lạ hoặc login bảo mật cao
        [Description("Đăng nhập 2 lớp")]
        Login2FA = 2,

        [Description("OTP có template chung dùng để xác thực")]
        General  = 3
    }
}