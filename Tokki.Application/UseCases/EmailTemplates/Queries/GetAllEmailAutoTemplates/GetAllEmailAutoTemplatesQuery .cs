using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EmailTemplates.Queries.GetAllEmailTemplates
{
    public class GetAllEmailAutoTemplatesQuery : IRequest<OperationResult<PagedResult<EmailTemplate>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Filters
        public EmailTemplateStatus? Status { get; set; }   // Draft/Active/Deleted
        public EmailTemplateType? Type { get; set; }       // OfflineReminder/VipExpiringReminder
        public UserTargetGroup? TargetGroup { get; set; }  // All/VipUsers/FreeUsers/None

        public int? Value { get; set; } // mốc thời gian

        // Search
        public string? SearchName { get; set; } // search TemplateName
        public string? SearchSubject { get; set; }
    }
}
