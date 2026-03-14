using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Commands.SyncWritingProgress
{
    public class SyncWritingProgressCommandHandler : IRequestHandler<SyncWritingProgressCommand, OperationResult<bool>>
    {
        private readonly IUserExamRepository _repository;

        public SyncWritingProgressCommandHandler(IUserExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(SyncWritingProgressCommand request, CancellationToken token)
        {
            var writing = await _repository.GetWritingAnswerWithSessionAsync(request.UserQuestionId, token);

            if (writing == null)
                return OperationResult<bool>.Failure("Không tìm thấy câu hỏi tự luận liên quan.", 404);

            if (writing.UserExam.UserId != request.UserId)
                return OperationResult<bool>.Failure("Bạn không có quyền lưu đáp án cho bài thi này.", 403);

            if (writing.UserExam.Status != UserExamStatus.InProgress)
                return OperationResult<bool>.Failure("Bài thi đã kết thúc, không thể cập nhật nội dung.", 400);

            if (writing.AnswerContent != request.AnswerContent)
            {
                writing.AnswerContent = request.AnswerContent ?? string.Empty;

                writing.WordCount = writing.AnswerContent.Length;

                await _repository.SaveChangesAsync(token);
            }

            return OperationResult<bool>.Success(true);
        }
    }
}
