using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;

namespace Tokki.Application.UseCases.ExamTemplates.Queries.GetAdminExamTemplates
{
    public class GetAdminExamTemplatesQueryHandler : IRequestHandler<GetAdminExamTemplatesQuery, OperationResult<PagedResult<AdminExamTemplateDto>>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;

        public GetAdminExamTemplatesQueryHandler(IExamTemplateRepository examTemplateRepository)
        {
            _examTemplateRepository = examTemplateRepository;
        }

        public async Task<OperationResult<PagedResult<AdminExamTemplateDto>>> Handle(GetAdminExamTemplatesQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _examTemplateRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Status,
                cancellationToken,
                request.Type
            );

            var dtos = items.Select(et => new AdminExamTemplateDto
            {
                ExamTemplateId = et.ExamTemplateId,
                Name = et.Name,
                Description = et.Description,
                CreatedAt = et.CreatedAt.AddHours(7),
                Status = et.Status,
                Type = et.Type,
                TotalParts = et.TemplateParts.Count,
                TotalQuestions = et.TemplateParts.Sum(p => p.QuestionTo - p.QuestionFrom + 1)
            }).ToList();

            var pagedResult = PagedResult<AdminExamTemplateDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<AdminExamTemplateDto>>.Success(pagedResult);
        }
    }
}