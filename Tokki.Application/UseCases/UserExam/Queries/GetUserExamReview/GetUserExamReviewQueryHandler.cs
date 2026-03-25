using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Entities;
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

            if (session == null)
                return OperationResult<UserExamReviewResponse>.Failure("Không tìm thấy bài làm.", 404);

            if (session.Status == UserExamStatus.InProgress)
                return OperationResult<UserExamReviewResponse>.Failure("Bài làm chưa nộp, không thể xem lại.", 400);

            var parts = session.Exam?.ExamTemplate?.TemplateParts ?? new List<TemplatePart>();

            (string Skill, double ScorePerQuestion) GetPartInfo(int orderIndex)
            {
                var part = parts.FirstOrDefault(p => orderIndex >= p.QuestionFrom && orderIndex <= p.QuestionTo);

                if (part == null) return ("Unknown", 0);

                return (part.Skill.ToString(), part.Mark);
            }

            double totalMaxScore = 0;
            var reviewQuestions = new List<ReviewQuestionDto>();

            foreach (var a in session.UserExamAnswers)
            {
                var info = GetPartInfo(a.OrderIndex);

                totalMaxScore += info.ScorePerQuestion;

                reviewQuestions.Add(new ReviewQuestionDto
                {
                    QuestionId = a.QuestionId,
                    OrderIndex = a.OrderIndex,
                    Content = a.Question.Content,
                    Explanation = a.Question.Explanation,
                    Skill = info.Skill,

                    QuestionMaxScore = info.ScorePerQuestion,

                    SelectedOptionId = a.SelectedOptionId,
                    IsCorrect = a.IsCorrect,
                    Options = a.Question.QuestionOptions.Select(o => new ReviewOptionDto
                    {
                        OptionId = o.OptionId,
                        Content = o.Content,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                });
            }

            foreach (var w in session.UserExamWritingAnswers)
            {
                var info = GetPartInfo(w.OrderIndex);

                totalMaxScore += info.ScorePerQuestion;

                reviewQuestions.Add(new ReviewQuestionDto
                {
                    QuestionId = w.QuestionId,
                    OrderIndex = w.OrderIndex,
                    Content = w.Question.Content,
                    Skill = "Writing",

                    QuestionMaxScore = info.ScorePerQuestion,

                    WritingAnswerContent = w.AnswerContent,
                    WritingScore = w.Score, 
                    AiAnalysisJson = w.AiAnalysisJson
                });
            }

            var response = new UserExamReviewResponse
            {
                UserExamId = session.UserExamId,
                ExamTitle = session.Exam?.Title ?? "Unknown Exam",

                TotalScore = session.Score,   
                MaxScore = totalMaxScore,     

                SubmitTime = session.SubmitTime,
                TimeSpentMinutes = session.SubmitTime.HasValue
                    ? (int)(session.SubmitTime.Value - session.StartTime).TotalMinutes
                    : 0,

                Questions = reviewQuestions.OrderBy(q => q.OrderIndex).ToList()
            };

            return OperationResult<UserExamReviewResponse>.Success(response);
        }
    }
}