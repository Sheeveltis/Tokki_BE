namespace Tokki.Application.UseCases.LiveChat.DTOs
{
    public class ChatRoomDTO
    {
        public string ChatRoomId { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RoomAvatar { get; set; } = string.Empty;
        public bool IsSupport { get; set; }
        public bool IsGroup { get; set; }
        public bool IsClosed { get; set; }
        public string? StaffName { get; set; }
        public string? LastMessage { get; set; }
        public DateTimeOffset? LastMessageTime { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}