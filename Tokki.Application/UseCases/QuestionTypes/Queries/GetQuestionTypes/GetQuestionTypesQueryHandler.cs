using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.QuestionTypes.Queries.GetQuestionTypes
{
    public class GetQuestionTypesQueryHandler : IRequestHandler<GetQuestionTypesQuery, OperationResult<IEnumerable<QuestionType>>>
    {
        private readonly IQuestionTypeRepository _repository;

        public GetQuestionTypesQueryHandler(IQuestionTypeRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<IEnumerable<QuestionType>>> Handle(GetQuestionTypesQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetAsync(
                request.Keyword,
                request.Skill,
                request.Difficulty,
                request.ExamType,
                cancellationToken
            );

            return OperationResult<IEnumerable<QuestionType>>.Success(result);
        }
    }
}