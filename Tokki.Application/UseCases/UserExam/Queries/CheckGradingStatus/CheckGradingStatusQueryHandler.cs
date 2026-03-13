using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;

namespace Tokki.Application.UseCases.UserExam.Queries.CheckGradingStatus
{
    public class CheckGradingStatusQueryHandler : IRequestHandler<CheckGradingStatusQuery, OperationResult<GradingStatusResponse>>
    {
        private readonly IUserExamRepository _repository;

        public CheckGradingStatusQueryHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<GradingStatusResponse>> Handle(CheckGradingStatusQuery request, CancellationToken token)
        {
            var isPending = await _repository.HasPendingWritingAnswersAsync(request.UserExamId, token);

            return OperationResult<GradingStatusResponse>.Success(new GradingStatusResponse
            {
                IsGraded = !isPending
            });
        }
    }
}
