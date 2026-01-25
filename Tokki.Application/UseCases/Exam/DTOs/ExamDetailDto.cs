using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.DTOs
{
    public class ExamDetailDto
    {
        public string ExamId { get; set; }
        public string Title { get; set; }
        public int Duration { get; set; }
        public  ExamType Type { get; set; }
        public ExamStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ExamPartDto> TemplateParts { get; set; } = new();
    }

    public class ExamPartDto
    {
        // Format: [[QuestionFrom]~[QuestionTo]] [Instruction] (각 [Mark] 점)
        public string TemplatePartId { get; set; }
        public string TemplatePartsTitle { get; set; }
        public string ExampleUrl { get; set; }
        public List<ExamQuestionDetailDto> Questions { get; set; } = new();
    }

    public class ExamQuestionDetailDto
    {
        public int QuestionNo { get; set; }
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

    public class QuestionOptionDto
    {
        public string KeyOption { get; set; } 
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public bool IsCorrect { get; set; }
    }
}
