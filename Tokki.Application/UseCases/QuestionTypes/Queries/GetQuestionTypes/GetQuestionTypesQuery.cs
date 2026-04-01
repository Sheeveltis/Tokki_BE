using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionTypes.Queries.GetQuestionTypes
{
    public class GetQuestionTypesQuery : IRequest<OperationResult<PagedResult<QuestionType>>>
    {
        public string? Keyword { get; set; }
        public QuestionSkill? Skill { get; set; }
        public DifficultyLevel? Difficulty { get; set; }
        public ExamType? ExamType { get; set; }
        public bool? IsActive { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}