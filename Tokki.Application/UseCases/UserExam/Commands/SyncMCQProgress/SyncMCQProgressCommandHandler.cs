using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Commands.SyncMCQProgress
{
    public class SyncMCQProgressCommandHandler : IRequestHandler<SyncMCQProgressCommand, OperationResult<bool>>
    {
        private readonly IUserExamRepository _repository;

        public SyncMCQProgressCommandHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(SyncMCQProgressCommand request, CancellationToken token)
        {
            if (request.Answers == null || !request.Answers.Any())
                return OperationResult<bool>.Success(true);

            var questionIds = request.Answers.Select(a => a.UserQuestionId).ToList();

            var userQuestions = await _repository.GetMCQAnswersByIdsAsync(questionIds, token);

            if (!userQuestions.Any())
                return OperationResult<bool>.Failure("Không tìm thấy các câu hỏi trắc nghiệm liên quan.", 404);

            var firstRecord = userQuestions.First();
            if (firstRecord.UserExam.UserId != request.UserId)
                return OperationResult<bool>.Failure("Bạn không có quyền thực hiện hành động này.", 403);

            if (firstRecord.UserExam.Status != UserExamStatus.InProgress)
                return OperationResult<bool>.Failure("Bài thi không còn trong trạng thái làm bài.", 400);

            bool isModified = false;
            foreach (var updateDto in request.Answers)
            {
                var currentQuestion = userQuestions.FirstOrDefault(q => q.UserExamAnswerId == updateDto.UserQuestionId);

                if (currentQuestion != null && currentQuestion.SelectedOptionId != updateDto.SelectedOptionId)
                {
                    currentQuestion.SelectedOptionId = updateDto.SelectedOptionId;

                    var correctOption = currentQuestion.Question.QuestionOptions.FirstOrDefault(o => o.IsCorrect);
                    currentQuestion.IsCorrect = currentQuestion.SelectedOptionId == correctOption?.OptionId;

                    isModified = true;
                }
            }

            if (isModified)
            {
                await _repository.SaveChangesAsync(token);
            }

            return OperationResult<bool>.Success(true);
        }
    }
}