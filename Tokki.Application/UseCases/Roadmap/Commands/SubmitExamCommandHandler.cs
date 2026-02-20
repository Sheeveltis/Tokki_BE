using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Exam.Commands.SubmitExam
{
    public class SubmitExamCommandHandler : IRequestHandler<SubmitExamCommand, OperationResult<int>>
    {
        private readonly IUserRoadmapRepository _repository; 
        private readonly IIdGeneratorService _idGenerator;

        public SubmitExamCommandHandler(IUserRoadmapRepository repository, IIdGeneratorService idGenerator)
        {
            _repository = repository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<int>> Handle(SubmitExamCommand request, CancellationToken cancellationToken)
        {
            var examQuestions = await _repository.GetExamQuestionsForGradingAsync(request.ExamId, cancellationToken);

            if (examQuestions == null || !examQuestions.Any())
            {
                return OperationResult<int>.Failure("Đề thi không tồn tại hoặc không có câu hỏi.", 404);
            }

            int totalScore = 0;
            var userExamId = _idGenerator.GenerateCustom(15);
            var examDetails = new List<UserExamDetail>();

            foreach (var userAnswer in request.Answers)
            {
                var questionEntity = examQuestions.FirstOrDefault(q => q.QuestionBankId == userAnswer.QuestionId);
                if (questionEntity == null) continue;

                var correctOption = questionEntity.QuestionBank.QuestionOptions.FirstOrDefault(o => o.IsCorrect);

                bool isCorrect = false;
                if (correctOption != null && correctOption.OptionId == userAnswer.SelectedOptionId)
                {
                    isCorrect = true;
                    totalScore += questionEntity.Score; 
                }

                examDetails.Add(new UserExamDetail
                {
                    DetailId = _idGenerator.GenerateCustom(15),
                    UserExamId = userExamId,
                    QuestionId = userAnswer.QuestionId,
                    SelectedOptionId = userAnswer.SelectedOptionId,
                    IsCorrect = isCorrect,
                    QuestionTypeId = questionEntity.QuestionBank.QuestionTypeId ?? "UNKNOWN"
                });
            }

            var userExam = new UserExam
            {
                UserExamId = userExamId,
                UserId = request.UserId,
                ExamId = request.ExamId,
                StartTime = DateTime.UtcNow.AddMinutes(-60), 
                SubmitTime = DateTime.UtcNow,
                Score = totalScore,
                Status = 1,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddUserExamAsync(userExam);
            await _repository.AddUserExamDetailsAsync(examDetails);

            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<int>.Success(totalScore, 200, "Nộp bài thành công!");
        }
    }
}