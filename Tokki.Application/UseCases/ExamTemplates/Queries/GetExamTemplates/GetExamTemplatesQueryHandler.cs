using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplates
{
    public class GetExamTemplatesQueryHandler : IRequestHandler<GetExamTemplatesQuery, OperationResult<PagedResult<ExamTemplateDto>>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;

        public GetExamTemplatesQueryHandler(IExamTemplateRepository examTemplateRepository)
        {
            _examTemplateRepository = examTemplateRepository;
        }

        public async Task<OperationResult<PagedResult<ExamTemplateDto>>> Handle(GetExamTemplatesQuery request, CancellationToken cancellationToken)
        {
            var statusFilter = request.Status ?? ExamTemplateStatus.Published;

            var (items, totalCount) = await _examTemplateRepository.GetPagedAsync(
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                searchTerm: request.SearchTerm,
                status: statusFilter,
                cancellationToken: cancellationToken,
                type: request.Type 
            );

            var dtos = items.Select(et => new ExamTemplateDto
            {
                ExamTemplateId = et.ExamTemplateId,
                Name = et.Name,
                Description = et.Description,
                CreatedAt = et.CreatedAt,
                Status = et.Status,
                TotalParts = et.TemplateParts.Count,
                TotalQuestions = et.TemplateParts.Sum(p => p.QuestionTo - p.QuestionFrom + 1),
                Parts = new List<TemplatePartDto>()
            }).ToList();

            var result = PagedResult<ExamTemplateDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize);

            return OperationResult<PagedResult<ExamTemplateDto>>.Success(result);
        }
    }
}