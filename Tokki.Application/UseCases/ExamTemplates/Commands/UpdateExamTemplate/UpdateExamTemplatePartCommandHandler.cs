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

            int newFrom = request.QuestionFrom ?? partToUpdate.QuestionFrom;
            int newTo = request.QuestionTo ?? partToUpdate.QuestionTo;
            var newSkill = request.Skill ?? partToUpdate.Skill;
            var newTypeId = request.QuestionTypeId ?? partToUpdate.QuestionTypeId;

            if (newFrom > newTo || newFrom <= 0)
                return OperationResult<bool>.Failure("Phạm vi câu hỏi không hợp lệ.");

            bool isOverlap = examTemplate.TemplateParts.Any(other =>
                other.TemplatePartId != request.TemplatePartId &&
                Math.Max(other.QuestionFrom, newFrom) <= Math.Min(other.QuestionTo, newTo)
            );
            if (isOverlap) return OperationResult<bool>.Failure("Phạm vi câu hỏi bị trùng lặp.");

            if (request.QuestionTypeId != null || request.Skill.HasValue)
            {
                if (!string.IsNullOrEmpty(newTypeId))
                {
                    var qt = await _questionTypeRepository.GetByIdAsync(newTypeId, cancellationToken);
                    if (qt == null) return OperationResult<bool>.Failure($"Loại câu hỏi không tồn tại.");
                    if (qt.Skill != newSkill) return OperationResult<bool>.Failure("Kỹ năng không khớp.");
                }
            }

            if (request.PartTitle != null) partToUpdate.PartTitle = request.PartTitle;
            if (request.Skill.HasValue) partToUpdate.Skill = request.Skill.Value;

            partToUpdate.QuestionFrom = newFrom;
            partToUpdate.QuestionTo = newTo;

            if (request.Instruction != null) partToUpdate.Instruction = request.Instruction;
            if (request.Mark.HasValue) partToUpdate.Mark = request.Mark.Value;
            if (request.QuestionTypeId != null) partToUpdate.QuestionTypeId = request.QuestionTypeId;
            if (request.ExampleUrl != null) partToUpdate.ExampleUrl = request.ExampleUrl;

            await _examTemplateRepository.UpdateAsync(examTemplate);
            await _examTemplateRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}