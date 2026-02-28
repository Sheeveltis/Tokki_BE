using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices; 
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Microsoft.AspNetCore.Http; 
using System.Security.Claims;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate
{
    public class CreateExamTemplateCommandHandler : IRequestHandler<CreateExamTemplateCommand, OperationResult<string>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CreateExamTemplateCommandHandler(
            IExamTemplateRepository examTemplateRepository,
            IIdGeneratorService idGeneratorService,
            IHttpContextAccessor httpContextAccessor)
        {
            _examTemplateRepository = examTemplateRepository;
            _idGeneratorService = idGeneratorService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<string>> Handle(CreateExamTemplateCommand request, CancellationToken cancellationToken)
        {
            if (await _examTemplateRepository.IsNameExistsAsync(request.Name))
            {
                return OperationResult<string>.Failure("Tên đề thi mẫu đã tồn tại.");
            }
            string userId = request.CreatedBy; 
            if (string.IsNullOrEmpty(userId))
            {
                userId = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            }
            if (string.IsNullOrEmpty(userId)) userId = "SYSTEM";

            var newId = _idGeneratorService.GenerateCustom(10);

            var examTemplate = new ExamTemplate
            {
                ExamTemplateId = newId,
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                Status = ExamTemplateStatus.Draft,
                CreatedBy = userId
            };

            await _examTemplateRepository.AddAsync(examTemplate);
            await _examTemplateRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(examTemplate.ExamTemplateId);
        }
    }
}