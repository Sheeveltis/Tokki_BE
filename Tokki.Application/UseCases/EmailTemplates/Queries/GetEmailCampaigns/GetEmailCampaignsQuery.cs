using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Queries.GetEmailCampaigns
{
    public class GetEmailCampaignsQuery : IRequest<OperationResult<PagedResult<EmailJob>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public EmailJobStatus? Status { get; set; }
        public UserTargetGroup? TargetGroup { get; set; }

        public DateTime? ScheduledFrom { get; set; }
        public DateTime? ScheduledTo { get; set; }

        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }

        public string? SearchSubject { get; set; }

        // mặc định false: không lấy Deleted
        public bool IncludeDeleted { get; set; } = false;
    }
}
