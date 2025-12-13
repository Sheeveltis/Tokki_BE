using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.DTOs
{
    public class TemplatePartDto
    {
        public string TemplatePartId { get; set; } = string.Empty;
        public QuestionSkill Skill { get; set; }
        public int QuestionFrom { get; set; }
        public int QuestionTo { get; set; }
        public string? PartTitle { get; set; }
        public string? Instruction { get; set; }
        public ExampleType ExampleType { get; set; }
        public string? ExampleData { get; set; }
    }
}
