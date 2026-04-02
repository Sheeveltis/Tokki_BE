using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.UserExam.DTOs
{
    public class UserTakeExamResponse
    {
        public string UserExamId { get; set; } = string.Empty;
        public string ExamId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int TotalQuestions { get; set; }
        public int TimeRemaining { get; set; }
        public Dictionary<string, int> SkillDurations { get; set; } = new();
        public Dictionary<string, int> SkillTimeRemaining { get; set; } = new();
        public string CurrentSkill { get; set; } = string.Empty;
        public int SkillTimeRemainingForCurrent { get; set; }

        public ExamSkillsDto Part { get; set; } = new();
    }

    public class ExamSkillsDto
    {
        public List<ExamPartDto> Listening { get; set; } = new();
        public List<ExamPartDto> Reading { get; set; } = new();
        public List<ExamPartWritingDto> Writing { get; set; } = new();
    }

    public class ExamPartDto
    {
        public string PartId { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ExampleUrl { get; set; }

        public List<QuestionGroupDto> QuestionGroups { get; set; } = new();
    }

    public class QuestionGroupDto
    {
        public string? SharedMediaUrl { get; set; }
        public string SharedMediaType { get; set; } = "None";
        public string? SharedPassageContent { get; set; }
        public string? SharedPassageMediaUrl { get; set; }

        public List<ExamQuestionDto> Questions { get; set; } = new();
    }

    public class ExamQuestionDto
    {
        public string UserQuestionId { get; set; } = string.Empty;
        public int QuestionNo { get; set; }
        public string Content { get; set; } = string.Empty;

        public List<ExamOptionDto> Options { get; set; } = new();
        public string? SelectedOptionId { get; set; }
    }

    public class ExamOptionDto
    {
        public string OptionId { get; set; } = string.Empty;
        public string KeyOption { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }


    public class ExamPartWritingDto
    {
        public string PartId { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ExampleUrl { get; set; }

        public List<QuestionWritingGroupDto> QuestionGroups { get; set; } = new();
    }

    public class QuestionWritingGroupDto
    {
        public string? SharedMediaUrl { get; set; }
        public string SharedMediaType { get; set; } = "None";
        public string? SharedPassageContent { get; set; }
        public string? SharedPassageMediaUrl { get; set; }

        public List<ExamQuestionWritingDto> Questions { get; set; } = new();
    }

    public class ExamQuestionWritingDto
    {
        public string UserQuestionId { get; set; } = string.Empty;
        public int QuestionNo { get; set; }
        public string Content { get; set; } = string.Empty;

        public string? AnswerContent { get; set; }
        public string? QuestionTypeCode { get; set; }
    }
}