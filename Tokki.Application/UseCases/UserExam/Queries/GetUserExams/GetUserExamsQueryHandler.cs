using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Queries.GetUserExams
{
    public class GetUserExamsQueryHandler : IRequestHandler<GetUserExamsQuery, OperationResult<PagedResult<UserExamActionDto>>>
    {
        private readonly IUserExamRepository _repository;

        public GetUserExamsQueryHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<PagedResult<UserExamActionDto>>> Handle(GetUserExamsQuery request, CancellationToken token)
        {
            var result = await _repository.GetPagedHistoryAsync(
                request.UserId,
                request.ExamId,
                request.Status,
                request.PageNumber,
                request.PageSize,
                token
            );

            return OperationResult<PagedResult<UserExamActionDto>>.Success(result);
        }
    }
}
