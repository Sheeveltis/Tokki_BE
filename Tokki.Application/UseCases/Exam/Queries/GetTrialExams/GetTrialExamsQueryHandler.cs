using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Queries.GetTrialExams
{
    public class GetTrialExamsQueryHandler : IRequestHandler<GetTrialExamsQuery, OperationResult<PagedResult<AdminExamDTO>>>
    {
        private readonly IExamRepository _examRepository;

        public GetTrialExamsQueryHandler(IExamRepository examRepository)
        {
            _examRepository = examRepository;
        }

        public async Task<OperationResult<PagedResult<AdminExamDTO>>> Handle(GetTrialExamsQuery request, CancellationToken cancellationToken)
        {
            // Chỉ lấy các đề có trạng thái TrialPublished
            var result = await _examRepository.GetPagedAsync(
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                searchTerm: request.SearchTerm,
                type: request.Type,
                status: ExamStatus.TrialPublished,
                cancellationToken: cancellationToken
            );

            var items = result.items;
            var totalCount = result.totalCount;

            var dtos = items.Select(e => new AdminExamDTO
            {
                ExamId = e.ExamId,
                ExamTemplateId = e.ExamTemplateId,
                ExamTemplateName = e.ExamTemplate?.Name,
                Title = e.Title,
                Type = e.Type,
                Status = e.Status,
                Duration = e.Duration,
                SkillDurations = e.SkillDurationsDict,
                CreatedAt = e.CreatedAt.AddHours(7),
                TotalQuestions = e.ExamQuestions?.Count ?? 0
            }).ToList();

            var pagedResult = PagedResult<AdminExamDTO>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<AdminExamDTO>>.Success(pagedResult);
        }
    }
}
