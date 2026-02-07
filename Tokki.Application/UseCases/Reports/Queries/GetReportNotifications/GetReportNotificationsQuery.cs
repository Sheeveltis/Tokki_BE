using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Reports.DTOs;

namespace Tokki.Application.UseCases.Reports.Queries.GetReportNotifications
{
    public class GetReportNotificationsQuery : IRequest<OperationResult<List<ReportNotificationDTO>>>
    {
        public string UserId { get; set; }
    }
}