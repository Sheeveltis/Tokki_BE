using System.Text.Json;
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
                return OperationResult<string>.Failure("Không tìm thấy campaign!", 404);

            if (job.Status != EmailJobStatus.Pending)
                return OperationResult<string>.Failure("Chỉ được cập nhật campaign khi trạng thái là Pending (chưa gửi).", 400);

            // ===== PATCH =====
            if (!string.IsNullOrWhiteSpace(request.Subject))
                job.Subject = request.Subject.Trim();

            if (!string.IsNullOrWhiteSpace(request.Body))
                job.Body = request.Body;

            if (request.TargetGroup.HasValue)
                job.TargetGroup = request.TargetGroup.Value;

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

            if (request.ScheduledTime.HasValue)
            {
                job.ScheduledTime = request.ScheduledTime.Value;
            }

            if (request.Status.HasValue)
            {
                if (request.Status.Value != EmailJobStatus.Deleted)
                    return OperationResult<string>.Failure("Chỉ cho phép cập nhật Status sang Deleted (xóa mềm).", 400);

                job.Status = EmailJobStatus.Deleted;

                // Nếu repo của bạn có SoftDeleteAsync và bạn muốn đồng bộ cơ chế soft delete:
                // await _emailJobRepository.SoftDeleteAsync(job);
                // (khi đó cân nhắc bỏ job.Status = Deleted nếu SoftDeleteAsync đã set)
            }

            // ===== Audit =====
            var now = DateTime.UtcNow.AddHours(7);
            job.UpdatedAt = now;
            job.UpdatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "system" : request.UpdatedBy;

            await _emailJobRepository.UpdateAsync(job);
            await _emailJobRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(job.JobId, 200, "Cập nhật campaign thành công!");
        }
    }
}
