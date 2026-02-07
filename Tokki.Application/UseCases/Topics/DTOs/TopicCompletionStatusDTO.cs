using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Topics.DTOs
{
    public class TopicCompletionStatusDTO
    {
        public string TopicId { get; set; }
        public bool IsCompleted { get; set; }
        public int ProgressPercent { get; set; } 
        public int TotalVocab { get; set; } 
        public int LearnedVocab { get; set; } 
    }
}
