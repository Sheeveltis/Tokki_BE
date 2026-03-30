using Hangfire;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Commands.SubmitUserExam
{
    public class SubmitUserExamCommandHandler : IRequestHandler<SubmitUserExamCommand, OperationResult<SubmitExamResponse>>
    {
        private readonly IUserExamRepository _repository;
        private readonly IBackgroundJobClient _backgroundJobs;
        public SubmitUserExamCommandHandler(
      IUserExamRepository repository,
      IBackgroundJobClient backgroundJobs)
        {
            _repository = repository;
            _backgroundJobs = backgroundJobs;
        }

        public async Task<OperationResult<SubmitExamResponse>> Handle(SubmitUserExamCommand request, CancellationToken token)
        {
            var session = await _repository.GetByIdAsync(request.UserExamId, token);

            if (session == null) return OperationResult<SubmitExamResponse>.Failure("Không tìm thấy phiên làm bài", 404);

            if (session.UserId != request.UserId)
            {
                return OperationResult<SubmitExamResponse>.Failure("Bạn không có quyền nộp bài thi này.", 403);
            }

            if (session.Status == UserExamStatus.Completed)
            {
                return OperationResult<SubmitExamResponse>.Failure("Bài thi này đã được nộp trước đó.", 400);
            }

            var now = DateTime.UtcNow;
            var maxDurationMinutes = session.Exam.Duration;
            var actualElapsedMinutes = (int)(now - session.StartTime).TotalMinutes;

            if (actualElapsedMinutes > maxDurationMinutes + 2)
            {
                session.SubmitTime = session.StartTime.AddMinutes(maxDurationMinutes);
            }
            else
            {
                session.SubmitTime = now;
            }

            var timeSpent = (int)(session.SubmitTime.Value - session.StartTime).TotalMinutes;
            timeSpent = Math.Clamp(timeSpent, 0, maxDurationMinutes);

            // Calculate MCQ Score based on TemplatePart marks
            int totalScore = 0;
            var templateParts = session.Exam.ExamTemplate.TemplateParts;
            var examQuestions = session.Exam.ExamQuestions;

            foreach (var part in templateParts)
            {
                var correctInPart = session.UserExamAnswers.Count(ua => 
                    examQuestions.Any(eq => eq.QuestionBankId == ua.QuestionId && eq.QuestionNo >= part.QuestionFrom && eq.QuestionNo <= part.QuestionTo) && 
                    ua.IsCorrect == true);
                
                totalScore += correctInPart * part.Mark;
            }

            session.Score = totalScore;
            session.Status = UserExamStatus.Completed;

            // Mark the last skill as finished upon submission
            var finishedList = string.IsNullOrEmpty(session.FinishedSkills)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(session.FinishedSkills) ?? new List<string>();

            if (!finishedList.Contains(session.CurrentSkill.ToString()))
            {
                finishedList.Add(session.CurrentSkill.ToString());
            }
            session.FinishedSkills = JsonSerializer.Serialize(finishedList);

            await _repository.SaveChangesAsync(token);

            aiGrading(request.UserExamId); // fire-and-forget an toàn vì chỉ enqueue, không query DB

            return OperationResult<SubmitExamResponse>.Success(new SubmitExamResponse
            {
                UserExamId = session.UserExamId,
                FinalMcqScore = totalScore,
                TimeSpentMinutes = timeSpent
            });
        }
        private void aiGrading(string userExamId)
        {
            _backgroundJobs.Enqueue<IWritingGradingBackgroundService>(
                s => s.GradeAllWritingByUserExamAsync(userExamId));
        }
    }
}
