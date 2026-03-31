using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.QuestionTypes.Queries.GetQuestionTypes
{
    public class GetQuestionTypesQueryHandler : IRequestHandler<GetQuestionTypesQuery, OperationResult<PagedResult<QuestionType>>>
    {
        private readonly IQuestionTypeRepository _repository;

        public GetQuestionTypesQueryHandler(IQuestionTypeRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<PagedResult<QuestionType>>> Handle(GetQuestionTypesQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.Keyword,
                request.Skill,
                request.Difficulty,
                request.ExamType,
                request.IsActive,
                cancellationToken
            );

            var pagedResult = PagedResult<QuestionType>.Create(
                items.ToList(),
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<QuestionType>>.Success(pagedResult);
        }
    }
}