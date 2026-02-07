using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;

namespace Tokki.Application.UseCases.TemplateParts.Queries.GetTemplateParts
{
    public class GetTemplatePartsQueryHandler : IRequestHandler<GetTemplatePartsQuery, OperationResult<PagedResult<TemplatePartDto>>>
    {
        private readonly ITemplatePartRepository _templatePartRepository;

        public GetTemplatePartsQueryHandler(ITemplatePartRepository templatePartRepository)
        {
            _templatePartRepository = templatePartRepository;
        }

        public async Task<OperationResult<PagedResult<TemplatePartDto>>> Handle(GetTemplatePartsQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _templatePartRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.ExamTemplateId,
                cancellationToken
            );

            var dtos = items.Select(part => new TemplatePartDto
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
                QuestionTypeName = part.QuestionType != null ? part.QuestionType.Name : "Chưa phân loại"
            }).ToList();

            var result = PagedResult<TemplatePartDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize);

            return OperationResult<PagedResult<TemplatePartDto>>.Success(result);
        }
    }
}