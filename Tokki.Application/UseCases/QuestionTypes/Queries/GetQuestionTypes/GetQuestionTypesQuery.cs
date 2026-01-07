using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionTypes.Queries.GetQuestionTypes
{
    public class GetQuestionTypesQuery : IRequest<OperationResult<IEnumerable<QuestionType>>>
    {
        public string? Keyword { get; set; }
        public QuestionSkill? Skill { get; set; }
        public DifficultyLevel? Difficulty { get; set; }
        public ExamType? ExamType { get; set; }
    }
}