using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Excel.DTOs
{
    public class ImportQuestionTypeResponse
    {
        public List<QuestionTypePreviewDTO> SuccessList { get; set; } = new();
        public List<QuestionTypePreviewDTO> FailureList { get; set; } = new();
    }

    public class QuestionTypePreviewDTO
    {
        public string Code { get; set; } = string.Empty; 
        public string Name { get; set; } = string.Empty; 
        public string Reason { get; set; } = string.Empty;
    }
}
