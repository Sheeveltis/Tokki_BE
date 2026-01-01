using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class GameMatch
    {
        [Key]
        public string LevelId { get; set; }  

        public string  GameId { get; set; }

        public string LevelName { get; set; } = null!; 

        public int LevelNo { get; set; }                
        public string  TopicId { get; set; }

        // Cấu hình linh động: số cặp thẻ, thời gian, rule tính điểm...
        // Lưu dạng JSON string
        //{
        //  "maxScore": 100,
        //  "scorePerCorrect": 10,
        //  "penaltyPerWrong": -5
        //}
        //var config = JsonSerializer.Deserialize<MatchConfig>(gameMatch.ConfigJson);

        public string? ConfigJson { get; set; }

        public int ExpRewardBase { get; set; }

        public GameMatchStatus Status { get; set; }  

        public Game Game { get; set; } = null!;

        public Topic Topic { get; set; } = null!;

        public ICollection<GameMatchSession> PlaySessions { get; set; } = new List<GameMatchSession>();
    }

}
