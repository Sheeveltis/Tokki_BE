using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Microsoft.AspNetCore.Http; 
using System.Security.Claims;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.DuplicateExamTemplate
{
    public class DuplicateExamTemplateCommandHandler : IRequestHandler<DuplicateExamTemplateCommand, OperationResult<string>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<DuplicateExamTemplateCommandHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DuplicateExamTemplateCommandHandler(
            IExamTemplateRepository examTemplateRepository,
            ITemplatePartRepository templatePartRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<DuplicateExamTemplateCommandHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _examTemplateRepository = examTemplateRepository;
            _templatePartRepository = templatePartRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<string>> Handle(DuplicateExamTemplateCommand request, CancellationToken cancellationToken)
        {
            var originalTemplate = await _examTemplateRepository.GetByIdWithPartsAsync(request.ExamTemplateId, cancellationToken);
            if (originalTemplate == null)
                return OperationResult<string>.Failure(AppErrors.ExamTemplateNotFound);

            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

                string newName = await GenerateDuplicateNameAsync(originalTemplate.Name);
                string newId = _idGeneratorService.GenerateCustom(10);

                var newTemplate = new ExamTemplate
                {
                    ExamTemplateId = newId,
                    Name = newName,
                    Description = originalTemplate.Description, 
                    Type = originalTemplate.Type,               
                    Status = ExamTemplateStatus.Draft,          
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    CreatedBy = userId
                };

                await _examTemplateRepository.AddAsync(newTemplate);

                if (originalTemplate.TemplateParts != null && originalTemplate.TemplateParts.Any())
                {
                    var newParts = originalTemplate.TemplateParts.Select(p => new TemplatePart
                    {
                        TemplatePartId = _idGeneratorService.GenerateCustom(10),
                        ExamTemplateId = newId, 
                        Skill = p.Skill,
                        QuestionFrom = p.QuestionFrom,
                        QuestionTo = p.QuestionTo,
                        PartTitle = p.PartTitle,
                        Instruction = p.Instruction,
                        Mark = p.Mark,
                        QuestionTypeId = p.QuestionTypeId,
                        ExampleUrl = p.ExampleUrl
                    }).ToList();

                    await _templatePartRepository.AddRangeAsync(newParts);
                }

                await _examTemplateRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(newId, 201, "Sao chép mẫu đề thi thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi sao chép mẫu đề thi: {Id}", request.ExamTemplateId);
                return OperationResult<string>.Failure(AppErrors.ServerError);
            }
        }
        private async Task<string> GenerateDuplicateNameAsync(string baseName)
        {
            string cleanName = Regex.Replace(baseName, @"\s\(\d+\)$", "").Trim();

            int count = 1;
            string newName = $"{cleanName} ({count})";

            while (await _examTemplateRepository.IsNameExistsAsync(newName))
            {
                count++;
                newName = $"{cleanName} ({count})";

                if (count > 100) break;
            }
            return newName;
        }
    }
}