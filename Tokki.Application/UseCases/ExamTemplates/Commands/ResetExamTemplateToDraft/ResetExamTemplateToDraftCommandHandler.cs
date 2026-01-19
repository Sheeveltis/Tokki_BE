using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.ResetExamTemplateToDraft
{
    public class ResetExamTemplateToDraftCommandHandler : IRequestHandler<ResetExamTemplateToDraftCommand, OperationResult<bool>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;

        public ResetExamTemplateToDraftCommandHandler(IExamTemplateRepository examTemplateRepository)
        {
            _examTemplateRepository = examTemplateRepository;
        }

        public async Task<OperationResult<bool>> Handle(ResetExamTemplateToDraftCommand request, CancellationToken cancellationToken)
        {
            var examTemplate = await _examTemplateRepository.GetByIdAsync(request.ExamTemplateId, cancellationToken);

            if (examTemplate == null)
                return OperationResult<bool>.Failure("Không tìm thấy đề thi mẫu.");

            if (examTemplate.Status != ExamTemplateStatus.Rejected)
            {
                return OperationResult<bool>.Failure("Chỉ có thể mở lại chỉnh sửa (về Nháp) khi đề thi đã bị Từ chối.");
            }

            examTemplate.Status = ExamTemplateStatus.Draft;

            await _examTemplateRepository.UpdateAsync(examTemplate);
            await _examTemplateRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}