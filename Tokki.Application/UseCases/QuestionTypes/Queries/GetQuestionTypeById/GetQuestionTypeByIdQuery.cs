using MediatR;
using Tokki.Application.Common.Models; 
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.QuestionTypes.Queries.GetQuestionTypeById
{
    public class GetQuestionTypeByIdQuery : IRequest<OperationResult<QuestionType>>
    {
        public string Id { get; set; } = string.Empty;

        public GetQuestionTypeByIdQuery(string id)
        {
            Id = id;
        }
    }
}