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
    public class CreateEmailCampaignCommand : IRequest<OperationResult<string>>
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public UserTargetGroup TargetGroup { get; set; } // Chọn Free/VIP/All
        public DateTime? ScheduledTime { get; set; } // Null = Gửi ngay
    }
}
