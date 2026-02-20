using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Queries.GetUserExamResult
{
    public class GetUserExamResultQueryHandler : IRequestHandler<GetUserExamResultQuery, OperationResult<UserExamResultResponse>>
    {
        private readonly IUserExamRepository _repository;

        public GetUserExamResultQueryHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<UserExamResultResponse>> Handle(GetUserExamResultQuery request, CancellationToken token)
        {
            var session = await _repository.GetResultWithDetailsAsync(request.UserExamId, token);

            if (session == null)
                return OperationResult<UserExamResultResponse>.Failure("Không tìm thấy kết quả bài thi.", 404);

            var response = new UserExamResultResponse
            {
                UserExamId = session.UserExamId,
                UserName = session.User?.FullName ?? "Học viên Tokki",
                ExamTitle = session.Exam?.Title ?? "N/A"
            };

            var templateParts = session.Exam?.ExamTemplate?.TemplateParts;

            if (templateParts == null || !templateParts.Any())
                return OperationResult<UserExamResultResponse>.Failure("Cấu trúc đề thi bị lỗi hoặc không tồn tại.", 400);

            response.Listening = MapSkillScore(session, templateParts, QuestionSkill.Listening);
            response.Reading = MapSkillScore(session, templateParts, QuestionSkill.Reading);
            response.Writing = MapSkillScore(session, templateParts, QuestionSkill.Writing);

            return OperationResult<UserExamResultResponse>.Success(response);
        }

        private SkillScoreDto MapSkillScore(Domain.Entities.UserExam session, IEnumerable<Domain.Entities.TemplatePart> parts, QuestionSkill targetSkill)
        {
            var skillParts = parts.Where(p => p.Skill == targetSkill).ToList();
            if (!skillParts.Any()) return new SkillScoreDto { IsGraded = true };

            double totalScore = 0;
            double maxScore = 0;
            int totalCorrect = 0;
            int totalQuestions = 0;
            bool isGraded = true; 

            foreach (var part in skillParts)
            {
                int from = part.QuestionFrom;
                int to = part.QuestionTo;
                int questionCount = to - from + 1;
                double mark = part.Mark;

                totalQuestions += questionCount;
                maxScore += questionCount * mark;

                if (targetSkill == QuestionSkill.Writing)
                {
                    var writingAnswers = session.UserExamWritingAnswers
                        .Where(a => a.OrderIndex >= from && a.OrderIndex <= to)
                        .ToList();

                    if (writingAnswers.Count < questionCount || writingAnswers.Any(a => a.Score == null))
                    {
                        isGraded = false;
                    }

                    totalCorrect += writingAnswers.Count(a => (a.Score ?? 0) > 0);
                    totalScore += writingAnswers.Sum(a => a.Score ?? 0);
                }
                else
                {
                    var mcqAnswers = session.UserExamAnswers
                        .Where(a => a.OrderIndex >= from && a.OrderIndex <= to)
                        .ToList();

                    int correctCount = mcqAnswers.Count(a => a.IsCorrect ?? false);
                    totalCorrect += correctCount;
                    totalScore += correctCount * mark;
                }
            }

            return new SkillScoreDto
            {
                TotalQuestions = totalQuestions,
                CorrectAnswers = totalCorrect,
                Score = totalScore,
                MaxScore = maxScore,
                IsGraded = isGraded
            };
        }
    }
}