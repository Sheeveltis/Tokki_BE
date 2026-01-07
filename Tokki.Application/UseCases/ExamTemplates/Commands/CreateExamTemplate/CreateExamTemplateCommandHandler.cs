using MediatR;
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
        private readonly IIdGeneratorService _idGeneratorService; 
        public CreateExamTemplateCommandHandler(
            IExamTemplateRepository examTemplateRepository,
            IIdGeneratorService idGeneratorService)
        {
            _examTemplateRepository = examTemplateRepository;
            _idGeneratorService = idGeneratorService;
        }

        public async Task<OperationResult<string>> Handle(CreateExamTemplateCommand request, CancellationToken cancellationToken)
        {
            if (await _examTemplateRepository.IsNameExistsAsync(request.Name))
            {
                return OperationResult<string>.Failure("Tên đề thi mẫu đã tồn tại.");
            }

            var newId = _idGeneratorService.GenerateCustom(10);

            var examTemplate = new ExamTemplate
            {
                ExamTemplateId = newId,
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                CreatedAt = DateTime.UtcNow,
                Status = ExamTemplateStatus.Draft
            };

            await _examTemplateRepository.AddAsync(examTemplate);
            await _examTemplateRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(examTemplate.ExamTemplateId);
        }
    }
}