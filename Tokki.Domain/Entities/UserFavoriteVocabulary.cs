using System;
using System.ComponentModel.DataAnnotations;

namespace Tokki.Domain.Entities
{
    /// <summary>
    /// Entity để lưu vocabulary yêu thích của user
    /// </summary>
    public class UserFavoriteVocabulary
    {
        [Key]
        public string FavoriteVocabularyId { get; set; } = string.Empty;
        
        public string UserId { get; set; } = string.Empty;
        
        public string VocabularyId { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Vocabulary Vocabulary { get; set; } = null!;
    }
}
