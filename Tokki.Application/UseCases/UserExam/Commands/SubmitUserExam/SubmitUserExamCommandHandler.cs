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

namespace Tokki.Application.UseCases.UserExam.Commands.SubmitUserExam
{
    public class SubmitUserExamCommandHandler : IRequestHandler<SubmitUserExamCommand, OperationResult<SubmitExamResponse>>
    {
        private readonly IUserExamRepository _repository;

        public SubmitUserExamCommandHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<SubmitExamResponse>> Handle(SubmitUserExamCommand request, CancellationToken token)
        {
            var session = await _repository.GetByIdAsync(request.UserExamId, token);
            if (session == null) return OperationResult<SubmitExamResponse>.Failure("Không tìm thấy phiên làm bài", 404);

            session.SubmitTime = DateTime.UtcNow;
            var timeSpent = (int)(session.SubmitTime.Value - session.StartTime).TotalMinutes;
            if (timeSpent > session.Exam.Duration) timeSpent = session.Exam.Duration;

            int totalScore = session.UserExamAnswers.Count(x => x.IsCorrect == true);
            session.Score = totalScore;
            session.Status = UserExamStatus.Completed;

            await _repository.SaveChangesAsync(token);

            return OperationResult<SubmitExamResponse>.Success(new SubmitExamResponse
            {
                UserExamId = session.UserExamId,
                FinalMcqScore = totalScore,
                TimeSpentMinutes = timeSpent
            });
        }
    }
}
