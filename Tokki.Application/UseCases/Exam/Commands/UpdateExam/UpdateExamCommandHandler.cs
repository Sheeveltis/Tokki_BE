using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Exam.Commands.UpdateExam
{
    public class UpdateExamCommandHandler : IRequestHandler<UpdateExamCommand, OperationResult<string>>
    {
        private readonly IExamRepository _examRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateExamCommandHandler(
            IExamRepository examRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _examRepository = examRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<string>> Handle(UpdateExamCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken);
            if (exam == null)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ExamNotFound },
                    404,
                    AppErrors.ExamNotFound.Description
                );
            }

            try
            {
                bool changed = false;

                // Title: null/"" => bỏ qua
                if (!string.IsNullOrWhiteSpace(request.Title))
                {
                    var newTitle = request.Title.Trim();

                    if (!string.Equals(exam.Title, newTitle, StringComparison.Ordinal))
                    {
                        bool titleExists = await _examRepository.IsTitleExistsAsync(newTitle, request.ExamId);
                        if (titleExists)
                        {
                            return OperationResult<string>.Failure(
                                new List<Error> { AppErrors.ExamTitleDuplicated },
                                409,
                                AppErrors.ExamTitleDuplicated.Description
                            );
                        }

                        exam.Title = newTitle;
                        changed = true;
                    }
                }

                // Duration: null => bỏ qua
                if (request.Duration.HasValue && exam.Duration != request.Duration.Value)
                {
                    exam.Duration = request.Duration.Value;
                    changed = true;
                }

                // Type: null => bỏ qua
                if (request.Type.HasValue && exam.Type != request.Type.Value)
                {
                    exam.Type = request.Type.Value;
                    changed = true;
                }

                // Status: null => bỏ qua
                if (request.Status.HasValue && exam.Status != request.Status.Value)
                {
                    exam.Status = request.Status.Value;
                    changed = true;
                }

                if (!changed)
                {
                    return OperationResult<string>.Success(
                        request.ExamId,
                        200,
                        "Không có thay đổi nào để cập nhật"
                    );
                }

                await _examRepository.UpdateAsync(exam);
                await _examRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    request.ExamId,
                    200,
                    "Cập nhật bài test thành công"
                );
            }
            catch
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
