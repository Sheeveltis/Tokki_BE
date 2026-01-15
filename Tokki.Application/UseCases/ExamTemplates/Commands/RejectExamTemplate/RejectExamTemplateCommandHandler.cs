using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.ExamTemplates.Commands.RejectExamTemplate
{
    public class RejectExamTemplateCommandHandler : IRequestHandler<RejectExamTemplateCommand, OperationResult<bool>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly IAccountRepository _accountRepository; 
        private readonly IEmailService _emailService;
        private readonly ILogger<RejectExamTemplateCommandHandler> _logger;

        public RejectExamTemplateCommandHandler(
            IExamTemplateRepository examTemplateRepository,
            IAccountRepository accountRepository,
            IEmailService emailService,
            ILogger<RejectExamTemplateCommandHandler> logger)
        {
            _examTemplateRepository = examTemplateRepository;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(RejectExamTemplateCommand request, CancellationToken cancellationToken)
        {
            var examTemplate = await _examTemplateRepository.GetByIdAsync(request.ExamTemplateId, cancellationToken);

            if (examTemplate == null)
                return OperationResult<bool>.Failure("Không tìm thấy đề thi mẫu.");

            if (examTemplate.Status != ExamTemplateStatus.PendingApproval)
                return OperationResult<bool>.Failure("Đề thi này không ở trạng thái chờ duyệt.");

            if (string.IsNullOrWhiteSpace(request.Reason))
                return OperationResult<bool>.Failure("Vui lòng nhập lý do từ chối.");

            examTemplate.Status = ExamTemplateStatus.Rejected;

            if (!string.IsNullOrEmpty(examTemplate.CreatedBy))
            {
                try
                {
                    var creator = await _accountRepository.GetByIdAsync(examTemplate.CreatedBy);
                    if (creator != null && !string.IsNullOrEmpty(creator.Email))
                    {
                        await _emailService.SendExamTemplateRejectedAsync(
                            creator.Email,
                            creator.FullName,
                            examTemplate.Name,
                            request.Reason
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gửi email từ chối thất bại cho ExamTemplateId: {Id}", examTemplate.ExamTemplateId);
                }
            }

            await _examTemplateRepository.UpdateAsync(examTemplate);
            await _examTemplateRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}