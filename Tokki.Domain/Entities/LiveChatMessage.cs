using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Tokki.Domain.Entities
{
    public class LiveChatMessage
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)] 
        public string LiveChatMessageId { get; set; } = string.Empty; 

        [BsonElement("content")]
        public string Content { get; set; } = string.Empty;

        [BsonElement("roomId")]
        public string RoomId { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("senderId")]
        public string SenderId { get; set; } = string.Empty;

        [BsonElement("senderName")]
        public string SenderName { get; set; } = string.Empty;

        [BsonElement("senderAvatar")]
        public string SenderAvatar { get; set; } = string.Empty;
    }
}