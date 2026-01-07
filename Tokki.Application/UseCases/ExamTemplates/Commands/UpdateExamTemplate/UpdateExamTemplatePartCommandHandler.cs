using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.UpdateTemplatePart
{
    public class UpdateExamTemplatePartCommandHandler : IRequestHandler<UpdateExamTemplatePartCommand, OperationResult<bool>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly IQuestionTypeRepository _questionTypeRepository;

        public UpdateExamTemplatePartCommandHandler(
            IExamTemplateRepository examTemplateRepository,
            IQuestionTypeRepository questionTypeRepository)
        {
            _examTemplateRepository = examTemplateRepository;
            _questionTypeRepository = questionTypeRepository;
        }

        public async Task<OperationResult<bool>> Handle(UpdateExamTemplatePartCommand request, CancellationToken cancellationToken)
        {
            var examTemplate = await _examTemplateRepository.GetByIdWithPartsAsync(request.ExamTemplateId, cancellationToken);

            if (examTemplate == null)
                return OperationResult<bool>.Failure("Không tìm thấy đề thi mẫu.");

            if (examTemplate.Status != ExamTemplateStatus.Draft)
                return OperationResult<bool>.Failure("Chỉ được phép sửa phần thi khi đề thi đang ở trạng thái Nháp.");

            var partToUpdate = examTemplate.TemplateParts.FirstOrDefault(p => p.TemplatePartId == request.TemplatePartId);
            if (partToUpdate == null)
                return OperationResult<bool>.Failure("Không tìm thấy phần thi này trong đề thi.");

            if (request.QuestionFrom > request.QuestionTo || request.QuestionFrom <= 0)
                return OperationResult<bool>.Failure("Phạm vi câu hỏi không hợp lệ.");

            bool isOverlap = examTemplate.TemplateParts.Any(otherPart =>
                otherPart.TemplatePartId != request.TemplatePartId &&
                Math.Max(otherPart.QuestionFrom, request.QuestionFrom) <= Math.Min(otherPart.QuestionTo, request.QuestionTo)
            );
            if (isOverlap) return OperationResult<bool>.Failure("Phạm vi câu hỏi bị trùng lặp.");

            var qt = await _questionTypeRepository.GetByIdAsync(request.QuestionTypeId, cancellationToken);
            if (qt == null) return OperationResult<bool>.Failure($"Loại câu hỏi '{request.QuestionTypeId}' không tồn tại.");
            if (qt.Skill != request.Skill) return OperationResult<bool>.Failure("Kỹ năng không khớp với loại câu hỏi.");

            partToUpdate.PartTitle = request.PartTitle ?? "Part";
            partToUpdate.Skill = request.Skill;
            partToUpdate.QuestionFrom = request.QuestionFrom;
            partToUpdate.QuestionTo = request.QuestionTo;
            partToUpdate.Instruction = request.Instruction;
            partToUpdate.Mark = request.Mark;
            partToUpdate.QuestionTypeId = request.QuestionTypeId;
            partToUpdate.ExampleUrl = request.ExampleUrl;

            await _examTemplateRepository.UpdateAsync(examTemplate);
            await _examTemplateRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}