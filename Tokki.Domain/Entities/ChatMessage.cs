using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class ChatMessage
    {
        [Key]
        [MaxLength(21)]
        public string ChatMessageId { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)] 
        public string RoomId { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? SenderId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ChatMessageType Type { get; set; } = ChatMessageType.Text; 

        [ForeignKey("SenderId")]
        public virtual Account? Sender { get; set; }

        [ForeignKey("RoomId")]
        public virtual ChatRoom? Room { get; set; }

        [NotMapped]
        public string? SenderName { get; set; }

        [NotMapped]
        public string? SenderAvatar { get; set; }

    }
}