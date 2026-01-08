using Tokki.Application.UseCases.ExamTemplates.Commands.AddTemplateParts;
using Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    public static class ExamTemplateTestData
    {
        public static ExamTemplate GetDraftTemplate(string id = "TEMPLATE_01")
        {
            return new ExamTemplate
            {
                ExamTemplateId = id,
                Name = "Đề thi thử TOEIC 2024",
                Description = "Mô tả đề thi",
                Type = ExamType.TopikII,
                Status = ExamTemplateStatus.Draft,
                TemplateParts = new List<TemplatePart>()
            };
        }
        public static ExamTemplate GetPublishedTemplate(string id = "TEMPLATE_02")
        {
            return new ExamTemplate
            {
                ExamTemplateId = id,
                Name = "Đề thi chính thức",
                Status = ExamTemplateStatus.Published
            };
        }
        public static CreateExamTemplateCommand GetCreateCommand()
        {
            return new CreateExamTemplateCommand
            {
                Name = "New Exam Template",
                Description = "Description",
                Type = ExamType.TopikII,
            };
        }
        public static AddTemplatePartsCommand GetAddPartsCommand(string templateId)
        {
            return new AddTemplatePartsCommand
            {
                ExamTemplateId = templateId,
                Parts = new List<CreateTemplatePartDto>
                {
                    new CreateTemplatePartDto
                    {
                        PartTitle = "Part 1",
                        Skill = QuestionSkill.Reading,
                        QuestionFrom = 1,
                        QuestionTo = 10,
                        QuestionTypeId = "QT_01",
                        Mark = 5
                    }
                }
            };
        }
        public static QuestionType GetQuestionType(string id, QuestionSkill skill)
        {
            return new QuestionType
            {
                QuestionTypeId = id,
                Name = "Multiple Choice",
                Skill = skill
            };
        }
    }
}