using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailCampaign
{
    public class DeleteEmailCampaignCommandHandler
         : IRequestHandler<DeleteEmailCampaignCommand, OperationResult<string>>
    {
        private readonly IEmailJobRepository _repo;

        public DeleteEmailCampaignCommandHandler(IEmailJobRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<string>> Handle(DeleteEmailCampaignCommand request, CancellationToken cancellationToken)
        {
            var job = await _repo.GetByIdAsync(request.JobId);
            if (job == null)
                return OperationResult<string>.Failure("Không tìm thấy campaign!", 404);

            // Chỉ cho xóa khi Pending (chưa gửi)
            if (job.Status != EmailJobStatus.Pending)
                return OperationResult<string>.Failure("Chỉ được xóa campaign khi trạng thái là Pending (chưa gửi).", 400);

            var now = DateTime.UtcNow.AddHours(7);

            // Audit: coi delete là một lần update
            job.UpdatedAt = now;
            job.UpdatedBy = request.UpdateBy; 

            await _repo.SoftDeleteAsync(job);
            await _repo.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(job.JobId, 200, "Xóa campaign (soft delete) thành công!");
        }
    }
}
