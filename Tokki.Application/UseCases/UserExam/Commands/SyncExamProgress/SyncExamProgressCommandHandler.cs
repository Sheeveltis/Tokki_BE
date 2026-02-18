using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;

namespace Tokki.Application.UseCases.UserExam.Commands.SyncExamProgress
{
    public class SyncExamProgressCommandHandler : IRequestHandler<SyncExamProgressCommand, OperationResult<bool>>
    {
        private readonly IUserExamRepository _repository;

        public SyncExamProgressCommandHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }
        public async Task<OperationResult<bool>> Handle(SyncExamProgressCommand request, CancellationToken token)
        {
            if (request.Answers == null || !request.Answers.Any())
                return OperationResult<bool>.Success(true);

            var incomingIds = request.Answers.Select(a => a.UserQuestionId).ToList();

            var session = await _repository.GetByAnswerIdAsync(incomingIds.First(), token);

            if (session == null)
                return OperationResult<bool>.Failure("Không tìm thấy phiên làm bài liên quan.", 404);

            if (session.UserId != request.UserId)
            {
                return OperationResult<bool>.Failure("Bạn không có quyền lưu đáp án cho bài thi này.", 403);
            }

            if (session.Status != Tokki.Domain.Enums.UserExamStatus.InProgress)
            {
                return OperationResult<bool>.Failure("Bài thi đã kết thúc, không thể lưu thêm đáp án.", 400);
            }

            var mcqMap = session.UserExamAnswers.ToDictionary(x => x.UserExamAnswerId);
            var writingMap = session.UserExamWritingAnswers.ToDictionary(x => x.UserExamWritingAnswerId);

            foreach (var incoming in request.Answers)
            {
                if (mcqMap.TryGetValue(incoming.UserQuestionId, out var mcq))
                {
                    if (mcq.SelectedOptionId != incoming.SelectedOptionId)
                    {
                        mcq.SelectedOptionId = incoming.SelectedOptionId;
                        var correctId = mcq.Question.QuestionOptions.FirstOrDefault(o => o.IsCorrect)?.OptionId;
                        mcq.IsCorrect = (mcq.SelectedOptionId == correctId);
                    }
                }
                else if (writingMap.TryGetValue(incoming.UserQuestionId, out var writing))
                {
                    if (writing.AnswerContent != incoming.AnswerContent)
                    {
                        writing.AnswerContent = incoming.AnswerContent ?? string.Empty;
                        writing.WordCount = writing.AnswerContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                    }
                }
            }

            await _repository.SaveChangesAsync(token);
            return OperationResult<bool>.Success(true);
        }

    }
}
