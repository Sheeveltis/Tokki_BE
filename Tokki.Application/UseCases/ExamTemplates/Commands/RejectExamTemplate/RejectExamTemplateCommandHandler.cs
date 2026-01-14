using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.RejectExamTemplate
{
    public class RejectExamTemplateCommandHandler : IRequestHandler<RejectExamTemplateCommand, OperationResult<bool>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;

        public RejectExamTemplateCommandHandler(IExamTemplateRepository examTemplateRepository)
        {
            _examTemplateRepository = examTemplateRepository;
        }

        public async Task<OperationResult<bool>> Handle(RejectExamTemplateCommand request, CancellationToken cancellationToken)
        {
            var examTemplate = await _examTemplateRepository.GetByIdAsync(request.ExamTemplateId, cancellationToken);

            if (examTemplate == null)
                return OperationResult<bool>.Failure("Không tìm thấy đề thi mẫu.");

            if (string.IsNullOrWhiteSpace(request.Reason))
                return OperationResult<bool>.Failure("Vui lòng nhập lý do từ chối phê duyệt.");

            examTemplate.Status = ExamTemplateStatus.Rejected;
            examTemplate.RejectReason = request.Reason;

            await _examTemplateRepository.UpdateAsync(examTemplate);
            await _examTemplateRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}