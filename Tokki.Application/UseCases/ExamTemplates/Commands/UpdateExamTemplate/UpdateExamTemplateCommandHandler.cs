using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate
{
    public class UpdateExamTemplateCommandHandler : IRequestHandler<UpdateExamTemplateCommand, OperationResult<string>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<UpdateExamTemplateCommandHandler> _logger;

        public UpdateExamTemplateCommandHandler(
            IExamTemplateRepository examTemplateRepository,
            ITemplatePartRepository templatePartRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<UpdateExamTemplateCommandHandler> logger)
        {
            _examTemplateRepository = examTemplateRepository;
            _templatePartRepository = templatePartRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(UpdateExamTemplateCommand request, CancellationToken cancellationToken)
        {
            var examTemplate = await _examTemplateRepository.GetByIdWithPartsAsync(request.ExamTemplateId, cancellationToken);
            if (examTemplate == null) return OperationResult<string>.Failure(AppErrors.ExamTemplateNotFound);

            if (examTemplate.Status == ExamTemplateStatus.Published)
            {
                bool isUsed = await _examTemplateRepository.HasExamsAsync(request.ExamTemplateId, cancellationToken);
                if (isUsed)
                {
                    return OperationResult<string>.Failure(AppErrors.ExamTemplateCannotUpdateInUse);
                }
            }

            if (await _examTemplateRepository.IsNameExistsAsync(request.Name, request.ExamTemplateId))
                return OperationResult<string>.Failure(AppErrors.ExamTemplateNameDuplicated);

            if (request.Parts.Count == 0) return OperationResult<string>.Failure(AppErrors.ExamTemplateNoParts);

            var sortedParts = request.Parts.OrderBy(p => p.QuestionFrom).ToList();
            for (int i = 0; i < sortedParts.Count; i++)
            {
                if (sortedParts[i].QuestionFrom > sortedParts[i].QuestionTo)
                    return OperationResult<string>.Failure($"Phần {i + 1}: {AppErrors.TemplatePartInvalidRange.Description}");
                if (i < sortedParts.Count - 1 && sortedParts[i].QuestionTo >= sortedParts[i + 1].QuestionFrom)
                    return OperationResult<string>.Failure($"Phần {i + 1}: {AppErrors.TemplatePartRangeOverlap.Description}");
            }

            try
            {
                examTemplate.Name = request.Name;
                examTemplate.Description = request.Description;
                examTemplate.Status = request.Status;
                examTemplate.Type = request.Type; 

                await _examTemplateRepository.UpdateAsync(examTemplate);

                var existingParts = examTemplate.TemplateParts.ToList();
                await _templatePartRepository.DeleteRangeAsync(existingParts);

                var newParts = request.Parts.Select(p => new TemplatePart
                {
                    TemplatePartId = string.IsNullOrEmpty(p.TemplatePartId) ? _idGeneratorService.GenerateCustom(10) : p.TemplatePartId,
                    ExamTemplateId = request.ExamTemplateId,
                    Skill = p.Skill,
                    QuestionFrom = p.QuestionFrom,
                    QuestionTo = p.QuestionTo,
                    PartTitle = p.PartTitle ?? string.Empty,
                    Instruction = p.Instruction,
                    Mark = p.Mark,
                    QuestionTypeId = p.QuestionTypeId,
                    ExampleUrl = p.ExampleUrl
                }).ToList();

                await _templatePartRepository.AddRangeAsync(newParts);
                await _examTemplateRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(request.ExamTemplateId, 200, "Cập nhật mẫu đề thi thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật mẫu đề thi");
                return OperationResult<string>.Failure(AppErrors.ServerError);
            }
        }
    }
}