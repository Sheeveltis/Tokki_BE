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

namespace Tokki.Application.UseCases.UserExam.Queries.GetUserExamReview
{
    public class GetUserExamReviewQueryHandler : IRequestHandler<GetUserExamReviewQuery, OperationResult<UserExamReviewResponse>>
    {
        private readonly IUserExamRepository _repository;

        public GetUserExamReviewQueryHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<UserExamReviewResponse>> Handle(GetUserExamReviewQuery request, CancellationToken cancellationToken)
        {
            var session = await _repository.GetReviewByIdAsync(request.UserExamId, cancellationToken);
            var parts = session.Exam.ExamTemplate.TemplateParts;

            string GetSkillName(int orderIndex)
            {
                var part = parts.FirstOrDefault(p => orderIndex >= p.QuestionFrom && orderIndex <= p.QuestionTo);
                return part?.Skill.ToString() ?? "Unknown";
            }
            if (session == null)
            {
                return OperationResult<UserExamReviewResponse>.Failure("Không tìm thấy bài làm này.", 404);
            }

            if (session.Status == UserExamStatus.InProgress)
            {
                return OperationResult<UserExamReviewResponse>.Failure("Bài làm đang trong tiến trình, chưa thể xem lại.", 400);
            }

            var response = new UserExamReviewResponse
            {
                UserExamId = session.UserExamId,
                ExamTitle = session.Exam.Title,
                TotalScore = session.Score,
                SubmitTime = session.SubmitTime,
                TimeSpentMinutes = session.SubmitTime.HasValue
                    ? (int)(session.SubmitTime.Value - session.StartTime).TotalMinutes
                    : 0
            };

            var mcqReview = session.UserExamAnswers.Select(a => new ReviewQuestionDto
            {
                QuestionId = a.QuestionId,
                OrderIndex = a.OrderIndex,
                Content = a.Question.Content,
                Explanation = a.Question.Explanation,

                Skill = GetSkillName(a.OrderIndex),

                SelectedOptionId = a.SelectedOptionId,
                IsCorrect = a.IsCorrect,
                Options = a.Question.QuestionOptions.Select(o => new ReviewOptionDto
                {
                    OptionId = o.OptionId,
                    Content = o.Content,
                    IsCorrect = o.IsCorrect
                }).ToList()
            });

            var writingReview = session.UserExamWritingAnswers.Select(w => new ReviewQuestionDto
            {
                QuestionId = w.QuestionId,
                OrderIndex = w.OrderIndex,
                Content = w.Question.Content,
                Explanation = w.Question.Explanation,
                Skill = "Writing",
                WritingAnswerContent = w.AnswerContent,
                WritingScore = w.Score,
                AiAnalysisJson = w.AiAnalysisJson
            });

            response.Questions = mcqReview.Concat(writingReview)
                                          .OrderBy(q => q.OrderIndex)
                                          .ToList();

            return OperationResult<UserExamReviewResponse>.Success(response);
        }
    }
}
