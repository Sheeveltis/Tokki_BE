using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class GuessResultDTO
    {
        public bool IsWon { get; set; }
        public bool IsGameOver { get; set; }
        public int AttemptCount { get; set; }
        public List<BlockFeedback> Feedbacks { get; set; } 
    }

    public class BlockFeedback
    {
        public char Character { get; set; } 
        public string BlockColor { get; set; } 
        public string InitialStatus { get; set; }
        public string VowelStatus { get; set; }  
        public string FinalStatus { get; set; } 
    }
}
