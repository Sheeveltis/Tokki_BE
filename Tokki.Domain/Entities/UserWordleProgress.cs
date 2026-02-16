using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Tokki.Domain.Entities
{
    public class UserWordleProgress
    {
        [Key]
        [MaxLength(20)]
        public string UserWordleProgressId { get; set; } = string.Empty;

        [MaxLength(15)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual Account User { get; set; } = null!; 

        [MaxLength(20)]
        public string DailyWordleId { get; set; } = string.Empty;

        [ForeignKey("DailyWordleId")]
        public virtual DailyWordle DailyWordle { get; set; } = null!;

        public string GuessesJson { get; set; } = "[]";

        [NotMapped]
        public List<string> Guesses
        {
            get => string.IsNullOrEmpty(GuessesJson)
                   ? new List<string>()
                   : JsonSerializer.Deserialize<List<string>>(GuessesJson) ?? new List<string>();
            set => GuessesJson = JsonSerializer.Serialize(value);
        }

        public int AttemptCount { get; set; }
        public bool IsWon { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }
}
