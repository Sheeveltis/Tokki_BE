using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;

namespace Tokki.Application.UseCases.Exam.Queries.GetExams
{
    public class GetExamsQueryHandler : IRequestHandler<GetExamsQuery, OperationResult<PagedResult<AdminExamDTO>>>
    {
        private readonly IExamRepository _examRepository;

        public GetExamsQueryHandler(IExamRepository examRepository)
        {
            _examRepository = examRepository;
        }

        public async Task<OperationResult<PagedResult<AdminExamDTO>>> Handle(GetExamsQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _examRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Type,       
                request.Status    
            );

            var dtos = items.Select(e => new AdminExamDTO
            {
                ExamId = e.ExamId,
                ExamTemplateId = e.ExamTemplateId,
                ExamTemplateName = e.ExamTemplate?.Name, 
                Title = e.Title,
                Type = e.Type,
                Status = e.Status,
                Duration = e.Duration,
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
