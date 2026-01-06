using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionTypes.Commands.CreateQuestionType
{
    public class CreateQuestionTypeCommand : IRequest<OperationResult<string>>
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public QuestionSkill Skill { get; set; }
        public DifficultyLevel Difficulty { get; set; } 
        public ExamType ExamType { get; set; }          
    }
}