using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Word.DTOs
{
    public class BulkCreateWordsResponse
    {
        public int TotalWords { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<WordCreationResult> Results { get; set; } = new();
    }

}
