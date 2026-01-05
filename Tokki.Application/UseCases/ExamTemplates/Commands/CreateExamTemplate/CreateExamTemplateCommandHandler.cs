using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate
{
    public class CreateExamTemplateCommandHandler : IRequestHandler<CreateExamTemplateCommand, OperationResult<string>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<CreateExamTemplateCommandHandler> _logger;

        public CreateExamTemplateCommandHandler(
            IExamTemplateRepository examTemplateRepository,
            ITemplatePartRepository templatePartRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<CreateExamTemplateCommandHandler> logger)
        {
            _examTemplateRepository = examTemplateRepository;
            _templatePartRepository = templatePartRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(CreateExamTemplateCommand request, CancellationToken cancellationToken)
        {
            if (await _examTemplateRepository.IsNameExistsAsync(request.Name))
                return OperationResult<string>.Failure(AppErrors.ExamTemplateNameDuplicated);

            if (request.Parts.Count == 0)
                return OperationResult<string>.Failure(AppErrors.ExamTemplateNoParts);

            var sortedParts = request.Parts.OrderBy(p => p.QuestionFrom).ToList();
            for (int i = 0; i < sortedParts.Count; i++)
            {
                if (sortedParts[i].QuestionFrom > sortedParts[i].QuestionTo)
                    return OperationResult<string>.Failure($"Phần '{sortedParts[i].PartTitle}': {AppErrors.TemplatePartInvalidRange.Description}");

                if (i < sortedParts.Count - 1 && sortedParts[i].QuestionTo >= sortedParts[i + 1].QuestionFrom)
                    return OperationResult<string>.Failure($"Phần '{sortedParts[i].PartTitle}': {AppErrors.TemplatePartRangeOverlap.Description}");
            }

            try
            {
                string examTemplateId = _idGeneratorService.GenerateCustom(10);
                var examTemplate = new ExamTemplate
                {
                    ExamTemplateId = examTemplateId,
                    Name = request.Name,
                    Description = request.Description,
                    Type = request.Type, 
                    Status = ExamTemplateStatus.Draft,
                    CreatedAt = DateTime.UtcNow
                };

                await _examTemplateRepository.AddAsync(examTemplate);

                var templateParts = request.Parts.Select(p => new TemplatePart
                {
                    TemplatePartId = _idGeneratorService.GenerateCustom(10),
                    ExamTemplateId = examTemplateId,
                    Skill = p.Skill,
                    QuestionFrom = p.QuestionFrom,
                    QuestionTo = p.QuestionTo,
                    PartTitle = p.PartTitle ?? string.Empty,
                    Instruction = p.Instruction,
                    Mark = p.Mark, 
                    QuestionTypeId = p.QuestionTypeId,
                    ExampleUrl = p.ExampleUrl 
                }).ToList();

                await _templatePartRepository.AddRangeAsync(templateParts);
                await _examTemplateRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(examTemplateId, 201, "Tạo mẫu đề thi thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo mẫu đề thi");
                return OperationResult<string>.Failure(AppErrors.ServerError);
            }
        }
    }
}