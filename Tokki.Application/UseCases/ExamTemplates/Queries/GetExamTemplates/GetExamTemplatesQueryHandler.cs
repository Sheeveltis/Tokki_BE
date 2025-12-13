using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.ExamTemplates.DTOs;

namespace Tokki.Application.UseCases.ExamTemplates.Queries.GetExamTemplates
{
    public class GetExamTemplatesQueryHandler : IRequestHandler<GetExamTemplatesQuery, OperationResult<PagedResult<ExamTemplateDto>>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;

        public GetExamTemplatesQueryHandler(IExamTemplateRepository examTemplateRepository)
        {
            _examTemplateRepository = examTemplateRepository;
        }

        public async Task<OperationResult<PagedResult<ExamTemplateDto>>> Handle(
            GetExamTemplatesQuery request,
            CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _examTemplateRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Status,
                cancellationToken
            );

            var dtos = items.Select(et => new ExamTemplateDto
            {
                ExamTemplateId = et.ExamTemplateId,
                Name = et.Name,
                Description = et.Description,
                CreatedAt = et.CreatedAt,
                Status = et.Status,
                TotalParts = et.TemplateParts.Count,
                TotalQuestions = et.TemplateParts.Any()
                    ? et.TemplateParts.Max(tp => tp.QuestionTo)
                    : 0,
                Parts = et.TemplateParts.Select(tp => new TemplatePartDto
                {
                    TemplatePartId = tp.TemplatePartId,
                    Skill = tp.Skill,
                    QuestionFrom = tp.QuestionFrom,
                    QuestionTo = tp.QuestionTo,
                    PartTitle = tp.PartTitle,
                    Instruction = tp.Instruction,
                    ExampleType = tp.ExampleType,
                    ExampleData = tp.ExampleData
                }).OrderBy(tp => tp.QuestionFrom).ToList()
            }).ToList();

            var pagedResult = PagedResult<ExamTemplateDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<ExamTemplateDto>>.Success(
                pagedResult,
                200,
                $"Tìm thấy {totalCount} mẫu đề thi."
            );
        }
    }
}
