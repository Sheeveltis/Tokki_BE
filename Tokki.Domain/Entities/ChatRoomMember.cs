using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    public class ChatRoomMember
    {
        [Key]
        [MaxLength(15)]
        public string ChatRoomMemberId { get; set; } 

        [Required]
        [MaxLength(10)]
        public string ChatRoomId { get; set; } = string.Empty;

        [ForeignKey("ChatRoomId")]
        public virtual ChatRoom ChatRoom { get; set; } = null!;

        [Required]
        [MaxLength(15)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual Account User { get; set; } = null!;

        public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

        public bool IsAdmin { get; set; } = false;
    }
}