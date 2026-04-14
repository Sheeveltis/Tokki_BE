using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Tokki.WebAPI.Hubs
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var user = connection.User;
            if (user == null) return null;

            // Ưu tiên NameIdentifier (chuẩn .NET)
            var nameId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(nameId)) return nameId;

            // Tiếp theo là "sub" (chuẩn JWT)
            var sub = user.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(sub)) return sub;

            // Nếu vẫn không thấy, tìm bất cứ thứ gì trông giống UserId (chứa gạch ngang như USER-)
            var fallback = user.Claims.FirstOrDefault(c => c.Value.Contains("-"))?.Value;
            
            return fallback;
        }
    }
}
