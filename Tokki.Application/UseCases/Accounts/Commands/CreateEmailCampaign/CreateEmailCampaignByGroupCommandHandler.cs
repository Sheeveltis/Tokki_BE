// Tokki.Application/UseCases/Email/Commands/CreateCampaign/CreateEmailCampaignByGroupCommandHandler.cs
using MediatR;
using System.Text.Json;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.CreateEmailCampaign;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Email.Commands.CreateCampaign
{
    public class CreateEmailCampaignByGroupCommandHandler : IRequestHandler<CreateEmailCampaignByGroupCommand, OperationResult<string>>
    {
        private readonly IEmailJobRepository _emailJobRepository;
        private readonly IIdGeneratorService _idGenerator;

        public CreateEmailCampaignByGroupCommandHandler(
            IEmailJobRepository emailJobRepository,
            IIdGeneratorService idGenerator)
        {
            _emailJobRepository = emailJobRepository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<string>> Handle(CreateEmailCampaignByGroupCommand request, CancellationToken cancellationToken)
        {
            var vnOffset = TimeSpan.FromHours(7);
            var nowVn = DateTimeOffset.UtcNow.ToOffset(vnOffset);

            // Lấy giờ user gửi (ưu tiên) hoặc lấy giờ hiện tại VN
            var sendTimeOffset = (request.ScheduledTime ?? nowVn).ToOffset(vnOffset);

            // Convert DateTimeOffset -> DateTime để lưu vào entity (DateTime)
            var sendTime = DateTime.SpecifyKind(sendTimeOffset.DateTime, DateTimeKind.Unspecified);


            // ✅ Chuyển danh sách email thành JSON
            string? specificEmailsJson = null;
            if (request.SpecificEmails != null && request.SpecificEmails.Any())
            {
                specificEmailsJson = JsonSerializer.Serialize(request.SpecificEmails);
            }

            var job = new EmailJob
            {
                CreatedBy = request.CreatedBy,
                JobId = _idGenerator.Generate(15),
                Subject = request.Subject,
                Body = request.Body,
                TargetGroup = request.TargetGroup,
                SpecificEmails = specificEmailsJson, 
                ScheduledTime = sendTime,
                Status = EmailJobStatus.Pending,
                CreatedAt = DateTime.SpecifyKind(nowVn.DateTime, DateTimeKind.Unspecified)
            };

            await _emailJobRepository.AddAsync(job);
            await _emailJobRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(job.JobId, 200, "Đã lên lịch gửi email thành công!");
        }
    }
}