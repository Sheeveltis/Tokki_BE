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

        public AddTemplatePartsCommandHandler(
            IExamTemplateRepository examTemplateRepository,
            IIdGeneratorService idGeneratorService)
        {
            _examTemplateRepository = examTemplateRepository;
            _idGeneratorService = idGeneratorService;
        }

        public async Task<OperationResult<bool>> Handle(AddTemplatePartsCommand request, CancellationToken cancellationToken)
        {
            var examTemplate = await _examTemplateRepository.GetByIdWithPartsAsync(request.ExamTemplateId, cancellationToken);

            if (examTemplate == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy đề thi mẫu.");
            }

            if (examTemplate.Status != ExamTemplateStatus.Draft)
            {
                return OperationResult<bool>.Failure("Chỉ được phép thêm phần thi khi đề thi đang ở trạng thái Nháp (Draft).");
            }

            if (request.Parts != null && request.Parts.Any())
            {
               
                foreach (var partDto in request.Parts)
                {
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

                    if (examTemplate.TemplateParts == null) examTemplate.TemplateParts = new List<TemplatePart>();
                    examTemplate.TemplateParts.Add(newPart);
                }

                await _examTemplateRepository.UpdateAsync(examTemplate);
                await _examTemplateRepository.SaveChangesAsync(cancellationToken);
            }

            return OperationResult<bool>.Success(true);
        }
    }
}