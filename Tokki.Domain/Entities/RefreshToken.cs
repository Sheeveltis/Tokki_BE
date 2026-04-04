// Domain/Entities/RefreshToken.cs
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Entities;

namespace Tokki.Domain.Entities
{
    [Table("RefreshTokens")] 
    public class RefreshToken
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("token_hash")]
        public string TokenHash { get; set; } = string.Empty;

        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        public Account User { get; set; } = null!;

        [Column("expiry_date")]
        public DateTime ExpiryDate { get; set; }

        [Column("revoked")]
        public bool Revoked { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}