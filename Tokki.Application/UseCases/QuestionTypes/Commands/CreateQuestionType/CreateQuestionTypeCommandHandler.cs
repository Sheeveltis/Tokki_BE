using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.QuestionTypes.Commands.CreateQuestionType
{
    public class CreateQuestionTypeCommandHandler : IRequestHandler<CreateQuestionTypeCommand, OperationResult<string>>
    {
        private readonly IQuestionTypeRepository _repository;
        private readonly IIdGeneratorService _idGenerator; 

        public CreateQuestionTypeCommandHandler(IQuestionTypeRepository repository, IIdGeneratorService idGenerator)
        {
            _repository = repository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<string>> Handle(CreateQuestionTypeCommand request, CancellationToken cancellationToken)
        {
            if (await _repository.IsNameExistsAsync(request.Name))
                return OperationResult<string>.Failure("Tên loại câu hỏi đã tồn tại.");

            if (!string.IsNullOrEmpty(request.Code) && await _repository.IsCodeExistsAsync(request.Code))
                return OperationResult<string>.Failure("Mã code đã tồn tại.");

            var entity = new QuestionType
            {
                QuestionTypeId = _idGenerator.GenerateCustom(10),
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                Skill = request.Skill,
                Difficulty = request.Difficulty, 
                ExamType = request.ExamType,     
                IsActive = true
            };

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(entity.QuestionTypeId, 201, "Tạo loại câu hỏi thành công");
        }
    }
}