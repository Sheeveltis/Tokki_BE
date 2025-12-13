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
            var sendTime = request.ScheduledTime ?? DateTime.UtcNow.AddHours(7);

            // ✅ Chuyển danh sách email thành JSON
            string? specificEmailsJson = null;
            if (request.SpecificEmails != null && request.SpecificEmails.Any())
            {
                specificEmailsJson = JsonSerializer.Serialize(request.SpecificEmails);
            }

            var job = new EmailJob
            {
                JobId = _idGenerator.Generate(15),
                Subject = request.Subject,
                Body = request.Body,
                TargetGroup = request.TargetGroup,
                SpecificEmails = specificEmailsJson, 
                ScheduledTime = sendTime,
                Status = EmailJobStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            await _emailJobRepository.AddAsync(job);
            await _emailJobRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(job.JobId, 200, "Đã lên lịch gửi email thành công!");
        }
    }
}