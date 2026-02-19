using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var answer = await _repository.GetMCQAnswerWithSessionAsync(request.UserQuestionId, token);

            if (answer == null)
                return OperationResult<bool>.Failure("Không tìm thấy câu hỏi trắc nghiệm liên quan.", 404);

            if (answer.UserExam.UserId != request.UserId)
                return OperationResult<bool>.Failure("Bạn không có quyền lưu đáp án cho bài thi này.", 403);

            if (answer.UserExam.Status != UserExamStatus.InProgress)
                return OperationResult<bool>.Failure("Bài thi đã kết thúc hoặc đã nộp, không thể lưu thêm.", 400);

            if (answer.SelectedOptionId != request.SelectedOptionId)
            {
                answer.SelectedOptionId = request.SelectedOptionId;

                var correctOption = answer.Question.QuestionOptions.FirstOrDefault(o => o.IsCorrect);
                answer.IsCorrect = answer.SelectedOptionId == correctOption?.OptionId;

                await _repository.SaveChangesAsync(token);
            }

            return OperationResult<bool>.Success(true);
        }
    }
}
