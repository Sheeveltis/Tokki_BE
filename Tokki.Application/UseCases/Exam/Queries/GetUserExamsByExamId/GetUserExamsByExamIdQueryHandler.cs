using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Queries.GetUserExamsByExamId
{
    public class GetUserExamsByExamIdQueryHandler : IRequestHandler<GetUserExamsByExamIdQuery, OperationResult<PagedResult<ExamParticipantDTO>>>
    {
        private readonly IUserExamRepository _userExamRepository;

        public GetUserExamsByExamIdQueryHandler(IUserExamRepository userExamRepository)
        {
            _userExamRepository = userExamRepository;
        }

        public async Task<OperationResult<PagedResult<ExamParticipantDTO>>> Handle(GetUserExamsByExamIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _userExamRepository.GetPagedParticipantsByExamIdAsync(
                request.ExamId,
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.IsDescending,
                cancellationToken
            );

            return OperationResult<PagedResult<ExamParticipantDTO>>.Success(result);
        }
    }
}
