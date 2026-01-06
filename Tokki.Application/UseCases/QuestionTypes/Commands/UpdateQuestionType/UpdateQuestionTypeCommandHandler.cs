using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.QuestionTypes.Commands.UpdateQuestionType
{
    public class UpdateQuestionTypeCommandHandler : IRequestHandler<UpdateQuestionTypeCommand, OperationResult<Unit>>
    {
        private readonly IQuestionTypeRepository _repository;

        public UpdateQuestionTypeCommandHandler(IQuestionTypeRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<Unit>> Handle(UpdateQuestionTypeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.QuestionTypeId, cancellationToken);
            if (entity == null)
                return OperationResult<Unit>.Failure("Không tìm thấy loại câu hỏi.");

            if (!string.IsNullOrWhiteSpace(request.Name)
                && request.Name != "string"
                && request.Name != entity.Name)
            {
                if (await _repository.IsNameExistsAsync(request.Name, request.QuestionTypeId))
                    return OperationResult<Unit>.Failure("Tên loại câu hỏi đã tồn tại.");

                entity.Name = request.Name;
            }

            if (!string.IsNullOrWhiteSpace(request.Code)
                && request.Code != "string"
                && request.Code != entity.Code)
            {
                if (await _repository.IsCodeExistsAsync(request.Code, request.QuestionTypeId))
                    return OperationResult<Unit>.Failure("Mã code đã tồn tại.");

                entity.Code = request.Code;
            }

            if (!string.IsNullOrWhiteSpace(request.Description) && request.Description != "string")
            {
                entity.Description = request.Description;
            }
            if (request.Skill > 0)
                entity.Skill = request.Skill;

            if (request.Difficulty > 0)
                entity.Difficulty = request.Difficulty;

            if (request.ExamType > 0)
                entity.ExamType = request.ExamType;

             entity.IsActive = request.Status == 1;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<Unit>.Success(Unit.Value, 200, "Cập nhật thành công");
        }
    }
}