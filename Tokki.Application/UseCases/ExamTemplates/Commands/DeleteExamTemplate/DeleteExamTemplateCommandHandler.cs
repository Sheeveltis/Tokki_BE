using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.DeleteExamTemplate
{
    public class DeleteExamTemplateCommandHandler : IRequestHandler<DeleteExamTemplateCommand, OperationResult<string>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly ILogger<DeleteExamTemplateCommandHandler> _logger;

        public DeleteExamTemplateCommandHandler(IExamTemplateRepository examTemplateRepository, ILogger<DeleteExamTemplateCommandHandler> logger)
        {
            _examTemplateRepository = examTemplateRepository;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(DeleteExamTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await _examTemplateRepository.GetByIdAsync(request.ExamTemplateId, cancellationToken);
            if (template == null || template.Status == ExamTemplateStatus.Deleted)
                return OperationResult<string>.Failure(AppErrors.ExamTemplateNotFound);

            try
            {
                template.Status = ExamTemplateStatus.Deleted;
                await _examTemplateRepository.UpdateAsync(template);
                await _examTemplateRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(request.ExamTemplateId, 200, "Xóa mẫu đề thi thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa mẫu đề thi: {Id}", request.ExamTemplateId);
                return OperationResult<string>.Failure(AppErrors.ServerError);
            }
        }
    }
}