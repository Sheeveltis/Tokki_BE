using System.ComponentModel.DataAnnotations;

namespace Tokki.Domain.Entities
{
    public class ChatRoom
    {
        [Key]
        [MaxLength(10)] 
        public string ChatRoomId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Name { get; set; } 

        public bool IsGroup { get; set; } = false;
        public bool IsSupport { get; set; } = false;

        public bool IsClosed { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual ICollection<ChatRoomMember> Members { get; set; } = new List<ChatRoomMember>();
    }
}