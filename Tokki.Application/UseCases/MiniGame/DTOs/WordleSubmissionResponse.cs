using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class WordleSubmissionResponse
    {
        public string SubmissionId { get; set; } = string.Empty;
        public string TargetWord { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public WordleAiFeedbackDto? AiFeedback { get; set; }
    }
}
