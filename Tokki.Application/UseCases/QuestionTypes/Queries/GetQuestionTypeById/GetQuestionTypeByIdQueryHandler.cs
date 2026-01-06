using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.QuestionTypes.Queries.GetQuestionTypeById
{
    public class GetQuestionTypeByIdQueryHandler : IRequestHandler<GetQuestionTypeByIdQuery, OperationResult<QuestionType>>
    {
        private readonly IQuestionTypeRepository _repository;

        public GetQuestionTypeByIdQueryHandler(IQuestionTypeRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<QuestionType>> Handle(GetQuestionTypeByIdQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Id))
            {
                return OperationResult<QuestionType>.Failure("ID không hợp lệ.");
            }

            var questionType = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (questionType == null)
            {
                return OperationResult<QuestionType>.Failure("Không tìm thấy loại câu hỏi.", 404);
            }

            return OperationResult<QuestionType>.Success(questionType);
        }
    }
}