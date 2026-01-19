using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.SubmitExamTemplate
{
    public class SubmitExamTemplateCommandHandler : IRequestHandler<SubmitExamTemplateCommand, OperationResult<bool>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;

        public SubmitExamTemplateCommandHandler(IExamTemplateRepository examTemplateRepository)
        {
            _examTemplateRepository = examTemplateRepository;
        }

        public async Task<OperationResult<bool>> Handle(SubmitExamTemplateCommand request, CancellationToken cancellationToken)
        {
            var examTemplate = await _examTemplateRepository.GetByIdWithPartsAsync(request.ExamTemplateId, cancellationToken);

            if (examTemplate == null)
                return OperationResult<bool>.Failure("Không tìm thấy đề thi mẫu.");

            if (examTemplate.Status != ExamTemplateStatus.Draft && examTemplate.Status != ExamTemplateStatus.Rejected)
            {
                return OperationResult<bool>.Failure("Trạng thái hiện tại không thể gửi duyệt.");
            }

            if (examTemplate.TemplateParts == null || !examTemplate.TemplateParts.Any())
            {
                return OperationResult<bool>.Failure("Đề thi chưa có nội dung phần thi.");
            }

            examTemplate.Status = ExamTemplateStatus.PendingApproval;

            await _examTemplateRepository.UpdateAsync(examTemplate);
            await _examTemplateRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}