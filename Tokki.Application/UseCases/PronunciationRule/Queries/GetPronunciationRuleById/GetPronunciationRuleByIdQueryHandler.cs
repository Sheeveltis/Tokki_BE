using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRuleById
{
    public class GetPronunciationRuleByIdQueryHandler : IRequestHandler<GetPronunciationRuleByIdQuery, OperationResult<PronunciationRuleDTO>>
    {
        private readonly IPronunciationRuleRepository _repository;

        public GetPronunciationRuleByIdQueryHandler(IPronunciationRuleRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<PronunciationRuleDTO>> Handle(GetPronunciationRuleByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.PronunciationRuleId);
            if (entity == null)
            {
                return OperationResult<PronunciationRuleDTO>.Failure("Không tìm thấy quy tắc phát âm.", 404);
            }

            var dto = new PronunciationRuleDTO
            {
                PronunciationRuleId = entity.PronunciationRuleId,
                RuleName = entity.RuleName,
                Description = entity.Description ?? "",
                Content = entity.Content ?? "",
                SortOrder = entity.SortOrder
            };

            return OperationResult<PronunciationRuleDTO>.Success(dto);
        }
    }
}
