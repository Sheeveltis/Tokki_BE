using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailCampaign
{
    public class UpdateEmailCampaignCommandHandler
        : IRequestHandler<UpdateEmailCampaignCommand, OperationResult<string>>
    {
        private readonly IEmailJobRepository _emailJobRepository;

        public UpdateEmailCampaignCommandHandler(IEmailJobRepository emailJobRepository)
        {
            _emailJobRepository = emailJobRepository;
        }

        public async Task<OperationResult<string>> Handle(UpdateEmailCampaignCommand request, CancellationToken cancellationToken)
        {
            var job = await _emailJobRepository.GetByIdAsync(request.JobId);
            if (job == null)
            {
                return OperationResult<string>.Failure("Không tìm thấy campaign!", 404);
            }

            // Chỉ cho update khi chưa gửi
            // Khuyến nghị: chỉ cho Pending; chặn Processing/Sent
            if (job.Status != EmailJobStatus.Pending)
            {
                return OperationResult<string>.Failure("Chỉ được cập nhật campaign khi trạng thái là Pending (chưa gửi).", 400);
            }

            // ===== Update theo kiểu PATCH: null/rỗng => giữ nguyên =====

            if (!string.IsNullOrWhiteSpace(request.Subject))
                job.Subject = request.Subject.Trim();

            if (!string.IsNullOrWhiteSpace(request.Body))
                job.Body = request.Body;

            if (request.TargetGroup.HasValue)
                job.TargetGroup = request.TargetGroup.Value;

            // SpecificEmails: chỉ update khi client truyền list != null
            // - nếu truyền []: hiểu là xoá danh sách email cá nhân
            if (request.SpecificEmails != null)
            {
                var cleaned = request.SpecificEmails
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                job.SpecificEmails = cleaned.Any()
                    ? JsonSerializer.Serialize(cleaned)
                    : null;
            }

            // ScheduledTime: chỉ update khi có value
            if (request.ScheduledTime.HasValue)
            {
                // đảm bảo Kind=Unspecified (giờ local UTC+7) để tránh lỗi offset
                job.ScheduledTime = DateTime.SpecifyKind(request.ScheduledTime.Value, DateTimeKind.Unspecified);
            }

            // Status: chỉ cho phép set Deleted (xóa mềm) trong update này
            if (request.Status.HasValue)
            {
                if (request.Status.Value != EmailJobStatus.Deleted)
                {
                    return OperationResult<string>.Failure("Chỉ cho phép cập nhật Status sang Deleted (xóa mềm).", 400);
                }

                job.Status = EmailJobStatus.Deleted;
            }

            await _emailJobRepository.UpdateAsync(job);
            await _emailJobRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(job.JobId, 200, "Cập nhật campaign thành công!");
        }
    }
}
