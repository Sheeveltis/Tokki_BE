using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.QuestionTypes.Commands.UpdateQuestionType
{
    public class UpdateQuestionTypeCommand : IRequest<OperationResult<Unit>>
    {
        [JsonIgnore]
        public string QuestionTypeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public QuestionSkill Skill { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public ExamType ExamType { get; set; }
        public int Status { get; set; }
    }
}