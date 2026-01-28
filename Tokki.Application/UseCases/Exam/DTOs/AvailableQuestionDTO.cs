using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Exam.DTOs
{
    public class AvailableQuestionDTO
    {
        public string QuestionBankId { get; set; }
        public string Content { get; set; }
        public string Explanation { get; set; }
        public string MediaUrl { get; set; }
        public string MediaType { get; set; }
        public string? PassageContent { get; set; }
        public string? PassageImageUrl { get; set; }
        public string? PassageAudioUrl { get; set; }
        public string? PassageMediaType { get; set; }

        public List<QuestionOptionDto> Options { get; set; } = new();
    }
}
