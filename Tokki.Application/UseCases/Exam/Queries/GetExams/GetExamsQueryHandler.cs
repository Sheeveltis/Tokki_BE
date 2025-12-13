using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;

namespace Tokki.Application.UseCases.Exam.Queries.GetExams
{
    public class GetExamsQueryHandler : IRequestHandler<GetExamsQuery, OperationResult<PagedResult<ExamSummaryDto>>>
    {
        private readonly IExamRepository _examRepository;

        public GetExamsQueryHandler(IExamRepository examRepository)
        {
            _examRepository = examRepository;
        }

        public async Task<OperationResult<PagedResult<ExamSummaryDto>>> Handle(
            GetExamsQuery request,
            CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _examRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Type,
                request.Status,
                request.ExamTemplateId,
                cancellationToken
            );

            var dtos = items.Select(e =>
            {
                var totalQuestions = e.ExamTemplate.TemplateParts.Any()
                    ? e.ExamTemplate.TemplateParts.Max(tp => tp.QuestionTo)
                    : 0;
                var completedQuestions = e.ExamQuestions.Count;
                var progress = totalQuestions > 0
                    ? (int)Math.Round((double)completedQuestions / totalQuestions * 100)
                    : 0;

                return new ExamSummaryDto
                {
                    ExamId = e.ExamId,
                    Title = e.Title,
                    Type = e.Type,
                    Status = e.Status,
                    ExamTemplateName = e.ExamTemplate.Name,
                    CreatedAt = e.CreatedAt,
                    TotalQuestions = totalQuestions,
                    CompletedQuestions = completedQuestions,
                    Progress = progress
                };
            }).ToList();

            var pagedResult = PagedResult<ExamSummaryDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<ExamSummaryDto>>.Success(
                pagedResult,
                200,
                $"Tìm thấy {totalCount} bài test."
            );
        }
    }
}
