using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories; // Using Interface
using Tokki.Application.UseCases.Accounts.Commands.CreateEmailCampaign;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Email.Commands.CreateCampaign
{
    public class CreateEmailCampaignByGroupCommandHandler : IRequestHandler<CreateEmailCampaignByGroupCommand, OperationResult<string>>
    {
        private readonly IEmailJobRepository _emailJobRepository;

        public CreateEmailCampaignByGroupCommandHandler(IEmailJobRepository emailJobRepository)
        {
            _emailJobRepository = emailJobRepository;
        }

        public async Task<OperationResult<string>> Handle(CreateEmailCampaignByGroupCommand request, CancellationToken cancellationToken)
        {
            var sendTime = request.ScheduledTime ?? DateTime.UtcNow.AddHours(7);

            var job = new EmailJob
            {
                Subject = request.Subject,
                Body = request.Body,
                TargetGroup = request.TargetGroup,
                ScheduledTime = sendTime,
                Status = EmailJobStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            // 3. Gọi Repository để lưu
            await _emailJobRepository.AddAsync(job);
            await _emailJobRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success("Đã lên lịch gửi email thành công!", 200);
        }
    }
}