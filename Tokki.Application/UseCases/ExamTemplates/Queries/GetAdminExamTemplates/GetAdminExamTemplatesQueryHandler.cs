using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;

namespace Tokki.Application.UseCases.ExamTemplates.Queries.GetAdminExamTemplates
{
    public class GetAdminExamTemplatesQueryHandler : IRequestHandler<GetAdminExamTemplatesQuery, OperationResult<(IEnumerable<ExamTemplateDto> Items, int TotalCount)>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;

        public GetAdminExamTemplatesQueryHandler(IExamTemplateRepository examTemplateRepository)
        {
            _examTemplateRepository = examTemplateRepository;
        }

        public async Task<OperationResult<(IEnumerable<ExamTemplateDto> Items, int TotalCount)>> Handle(GetAdminExamTemplatesQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _examTemplateRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Status,
                cancellationToken,
                request.Type
            );

            var dtos = items.Select(et => new ExamTemplateDto
            {
                ExamTemplateId = et.ExamTemplateId,
                Name = et.Name,
                Description = et.Description,
                Type = et.Type,
                CreatedAt = et.CreatedAt,
                Status = et.Status,
                TotalParts = et.TemplateParts.Count,
                TotalQuestions = et.TemplateParts.Sum(p => p.QuestionTo - p.QuestionFrom + 1)
            }).ToList();

            return OperationResult<(IEnumerable<ExamTemplateDto>, int)>.Success((dtos, totalCount));
        }
    }
}