using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.VocabularyExample.Commands.DeleteExample
{
    // Lưu ý: phải public để MediatR scan được handler
    public class DeleteVocabularyExampleCommandHandler
        : IRequestHandler<DeleteVocabularyExampleCommand, OperationResult<bool>>
    {
        private readonly IVocabularyExampleRepository _exampleRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DeleteVocabularyExampleCommandHandler> _logger;

        public DeleteVocabularyExampleCommandHandler(
            IVocabularyExampleRepository exampleRepo,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DeleteVocabularyExampleCommandHandler> logger)
        {
            _exampleRepo = exampleRepo;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            DeleteVocabularyExampleCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            if (string.IsNullOrWhiteSpace(request.ExampleId))
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ExampleIdEmpty },
                    400,
                    AppErrors.ExampleIdEmpty.Description
                );
            }

            // Repo interface của bạn hiện tại: GetByIdAsync(string exampleId) (không có CancellationToken)
            var example = await _exampleRepo.GetByIdAsync(request.ExampleId);
            if (example == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ExampleNotFound },
                    404,
                    AppErrors.ExampleNotFound.Description
                );
            }

            // Soft delete
            if (example.Status == VocabularyExampleStatus.Deleted)
            {
                return OperationResult<bool>.Success(true, 200, "Câu ví dụ đã ở trạng thái xóa");
            }

            example.Status = VocabularyExampleStatus.Deleted;

            // Vì entity đang được track (repo GetByIdAsync không AsNoTracking), chỉ cần SaveChanges
            // Nếu muốn rõ ràng hơn, có thể gọi UpdateAsync:
            await _exampleRepo.UpdateAsync(example);

            await _exampleRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Soft deleted example {ExampleId} by user {UserId}",
                request.ExampleId, currentUserId);

            return OperationResult<bool>.Success(true, 200, "Xóa câu ví dụ thành công");
        }
    }
}
