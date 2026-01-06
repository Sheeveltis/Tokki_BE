using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;

namespace Tokki.Application.UseCases.TemplateParts.Queries.GetTemplatePartById
{
    public class GetTemplatePartByIdQueryHandler : IRequestHandler<GetTemplatePartByIdQuery, OperationResult<TemplatePartDto>>
    {
        private readonly ITemplatePartRepository _templatePartRepository;

        public GetTemplatePartByIdQueryHandler(ITemplatePartRepository templatePartRepository)
        {
            _templatePartRepository = templatePartRepository;
        }

        public async Task<OperationResult<TemplatePartDto>> Handle(GetTemplatePartByIdQuery request, CancellationToken cancellationToken)
        {
            var cleanId = request.TemplatePartId?.Trim();
            var part = await _templatePartRepository.GetByIdAsync(cleanId, cancellationToken);
            if (part == null)
            {
                return OperationResult<TemplatePartDto>.Failure(AppErrors.TemplatePartNotFound);
            }

            var dto = new TemplatePartDto
            {
                TemplatePartId = part.TemplatePartId,
                Skill = part.Skill,
                QuestionFrom = part.QuestionFrom,
                QuestionTo = part.QuestionTo,
                PartTitle = part.PartTitle,
                Instruction = part.Instruction,
                Mark = part.Mark,
                ExampleUrl = part.ExampleUrl,
                QuestionTypeId = part.QuestionTypeId ?? string.Empty,
                QuestionTypeName = part.QuestionType != null ? part.QuestionType.Name : string.Empty
            };

            return OperationResult<TemplatePartDto>.Success(dto);
        }
    }
}