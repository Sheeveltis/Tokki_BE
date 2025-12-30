using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailCampaign
{
    public class UpdateEmailCampaignCommand : IRequest<OperationResult<string>>
    {
        public string JobId { get; set; } = string.Empty;

        // Optional fields: null / rỗng => giữ nguyên
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public UserTargetGroup? TargetGroup { get; set; }
        public List<string>? SpecificEmails { get; set; }
        public DateTime? ScheduledTime { get; set; }

        // Cho phép set Deleted để xóa mềm (optional)
        public EmailJobStatus? Status { get; set; }

        // Audit: không cho client gửi qua JSON
        [JsonIgnore]
        public string? UpdatedBy { get; set; }
    }
}
