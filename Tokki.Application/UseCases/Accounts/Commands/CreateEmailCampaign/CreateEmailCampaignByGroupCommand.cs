using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.CreateEmailCampaign
{
    public class CreateEmailCampaignByGroupCommand : IRequest<OperationResult<string>>
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public UserTargetGroup TargetGroup { get; set; }

        public List<string>? SpecificEmails { get; set; }

        public DateTime? ScheduledTime { get; set; }
    }
}
