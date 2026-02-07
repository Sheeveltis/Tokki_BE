using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.AddTemplateParts
{
    public class AddTemplatePartsCommandHandler : IRequestHandler<AddTemplatePartsCommand, OperationResult<bool>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IQuestionTypeRepository _questionTypeRepository;

        public AddTemplatePartsCommandHandler(
            IExamTemplateRepository examTemplateRepository,
            IIdGeneratorService idGeneratorService,
            IQuestionTypeRepository questionTypeRepository)
        {
            _examTemplateRepository = examTemplateRepository;
            _idGeneratorService = idGeneratorService;
            _questionTypeRepository = questionTypeRepository;
        }

        public async Task<OperationResult<bool>> Handle(AddTemplatePartsCommand request, CancellationToken cancellationToken)
        {
            var examTemplate = await _examTemplateRepository.GetByIdWithPartsAsync(request.ExamTemplateId, cancellationToken);

            if (examTemplate == null)
                return OperationResult<bool>.Failure("Không tìm thấy đề thi mẫu.");

            if (examTemplate.Status != ExamTemplateStatus.Draft)
                return OperationResult<bool>.Failure("Chỉ được phép thêm phần thi khi đề thi đang ở trạng thái Nháp (Draft).");

            if (examTemplate.TemplateParts == null) examTemplate.TemplateParts = new List<TemplatePart>();

            if (request.Parts != null && request.Parts.Any())
            {
                foreach (var partDto in request.Parts)
                {
                    if (partDto.QuestionFrom > partDto.QuestionTo)
                    {
                        return OperationResult<bool>.Failure($"Phạm vi câu hỏi không hợp lệ: {partDto.QuestionFrom} đến {partDto.QuestionTo}.");
                    }

                    if (partDto.QuestionFrom <= 0)
                    {
                        return OperationResult<bool>.Failure($"Số thứ tự câu hỏi phải lớn hơn 0.");
                    }

                    bool isOverlap = examTemplate.TemplateParts.Any(existingPart =>
                        Math.Max(existingPart.QuestionFrom, partDto.QuestionFrom) <= Math.Min(existingPart.QuestionTo, partDto.QuestionTo)
                    );

                    if (isOverlap)
                    {
                        return OperationResult<bool>.Failure($"Phạm vi câu hỏi {partDto.QuestionFrom}-{partDto.QuestionTo} bị trùng lặp với các phần thi đã có.");
                    }

                    var qt = await _questionTypeRepository.GetByIdAsync(partDto.QuestionTypeId, cancellationToken);
                    if (qt == null)
                    {
                        return OperationResult<bool>.Failure($"Loại câu hỏi với ID '{partDto.QuestionTypeId}' không tồn tại.");
                    }

                    if (qt.Skill != partDto.Skill)
                    {
                        return OperationResult<bool>.Failure($"Kỹ năng không khớp. Phần thi là '{partDto.Skill}' nhưng loại câu hỏi là '{qt.Skill}'.");
                    }

                    var newPart = new TemplatePart
                    {
                        TemplatePartId = _idGeneratorService.GenerateCustom(10),
                        ExamTemplateId = examTemplate.ExamTemplateId,
                        PartTitle = partDto.PartTitle ?? "Part",
                        Skill = partDto.Skill,
                        QuestionFrom = partDto.QuestionFrom,
                        QuestionTo = partDto.QuestionTo,
                        Instruction = partDto.Instruction,
                        Mark = partDto.Mark,
                        QuestionTypeId = partDto.QuestionTypeId,
                        ExampleUrl = partDto.ExampleUrl
                    };
                    examTemplate.TemplateParts.Add(newPart);
                }

                await _examTemplateRepository.UpdateAsync(examTemplate);
                await _examTemplateRepository.SaveChangesAsync(cancellationToken);
            }

            return OperationResult<bool>.Success(true);
        }
    }
}