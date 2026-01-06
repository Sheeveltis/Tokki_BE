using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.QuestionTypes.Commands.DeleteQuestionType
{
    public class DeleteQuestionTypeCommandHandler : IRequestHandler<DeleteQuestionTypeCommand, OperationResult<Unit>>
    {
        private readonly IQuestionTypeRepository _repository;

        public DeleteQuestionTypeCommandHandler(IQuestionTypeRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<Unit>> Handle(DeleteQuestionTypeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (entity == null)
            {
                return OperationResult<Unit>.Failure("Không tìm thấy loại câu hỏi để xóa.", 404);
            }         
            entity.IsActive = false; 

            try
            {
                await _repository.UpdateAsync(entity);
                await _repository.SaveChangesAsync(cancellationToken);

                return OperationResult<Unit>.Success(Unit.Value, 200, "Xóa thành công (Soft Delete).");
            }
            catch (Exception ex)
            {
                return OperationResult<Unit>.Failure($"Lỗi khi xóa: {ex.Message}", 500);
            }
        }
    }
}