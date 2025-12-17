using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.VocabSpacedRepetition.DTOs
{
    public class ReviewResponse
    {
        public string VocabularyId { get; set; }

        public bool IsMastered { get; set; }
    }
}
