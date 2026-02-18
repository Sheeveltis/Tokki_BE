using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class WordleResultDTO
    {
        public string DailyWordleId { get; set; }
        public string Word { get; set; }
        public string Definition { get; set; }
        public string ImageUrl { get; set; }
        public string AudioUrl { get; set; }
        public int AttemptCount { get; set; }
    }
}
