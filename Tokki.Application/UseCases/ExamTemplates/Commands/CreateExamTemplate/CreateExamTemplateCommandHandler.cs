using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            // Validate tên không trùng
            bool nameExists = await _examTemplateRepository.IsNameExistsAsync(request.Name);
            if (nameExists)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ExamTemplateNameDuplicated },
                    409,
                    AppErrors.ExamTemplateNameDuplicated.Description
                );
            }

            // Validate phải có ít nhất 1 part
            if (request.Parts.Count == 0)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ExamTemplateNoParts },
                    400,
                    AppErrors.ExamTemplateNoParts.Description
                );
            }

            // Validate range của từng part
            foreach (var part in request.Parts)
            {
                if (part.QuestionFrom > part.QuestionTo)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.TemplatePartInvalidRange },
                        400,
                        $"Phần '{part.PartTitle}': {AppErrors.TemplatePartInvalidRange.Description}"
                    );
                }

                if (part.QuestionFrom <= 0 || part.QuestionTo <= 0)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.TemplatePartQuestionRangeInvalid },
                        400,
                        $"Phần '{part.PartTitle}': {AppErrors.TemplatePartQuestionRangeInvalid.Description}"
                    );
                }
            }

            // Validate không có range bị overlap
            var sortedParts = request.Parts.OrderBy(p => p.QuestionFrom).ToList();
            for (int i = 0; i < sortedParts.Count - 1; i++)
            {
                if (sortedParts[i].QuestionTo >= sortedParts[i + 1].QuestionFrom)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.TemplatePartRangeOverlap },
                        400,
                        $"Phần '{sortedParts[i].PartTitle}' và '{sortedParts[i + 1].PartTitle}': {AppErrors.TemplatePartRangeOverlap.Description}"
                    );
                }
            }

            try
            {
                string examTemplateId = _idGeneratorService.GenerateCustom(10);

                var examTemplate = new ExamTemplate
                {
                    ExamTemplateId = examTemplateId,
                    Name = request.Name,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow,
                    Status = ExamTemplateStatus.Draft
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
                    DifficultyLevel = p.DifficultyLevel,
                    QuestionTypeId = p.QuestionTypeId,
                    ExampleType = p.ExampleType,
                    ExampleData = p.ExampleData
                }).ToList();

                await _templatePartRepository.AddRangeAsync(templateParts);
                await _examTemplateRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    examTemplateId,
                    201,
                    "Tạo mẫu đề thi thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo mẫu đề thi: {Name}", request.Name);
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
