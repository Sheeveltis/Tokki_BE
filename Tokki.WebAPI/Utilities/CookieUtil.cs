namespace Tokki.WebAPI.Utilities
{
    public static class CookieUtil
    {
        public static void SetRefreshTokenCookie(HttpResponse response, string rawToken, int maxAgeDays = 7)
        {
            // Guard clause — không set cookie nếu token rỗng
            if (string.IsNullOrEmpty(rawToken)) return;

            response.Cookies.Append("refreshToken", rawToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,        // ← đổi thành false khi dev HTTP, true khi production HTTPS
                SameSite = SameSiteMode.Strict,
                Path = "/",
                MaxAge = TimeSpan.FromDays(maxAgeDays)
            });
        }

        public static void ClearRefreshTokenCookie(HttpResponse response)
        {
            response.Cookies.Append("refreshToken", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = false,        // ← đổi giống trên
                Expires = DateTimeOffset.UtcNow.AddDays(-1),
                MaxAge = TimeSpan.Zero,
                Path = "/"
            });
        }
    }
}