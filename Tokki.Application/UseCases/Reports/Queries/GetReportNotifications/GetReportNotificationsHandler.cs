using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Reports.DTOs;

namespace Tokki.Application.UseCases.Reports.Queries.GetReportNotifications
{
    public class GetReportNotificationsHandler : IRequestHandler<GetReportNotificationsQuery, OperationResult<List<ReportNotificationDTO>>>
    {
        private readonly IReportRepository _reportRepository;

        public GetReportNotificationsHandler(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<OperationResult<List<ReportNotificationDTO>>> Handle(GetReportNotificationsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var reports = await _reportRepository.GetUnreadResolvedReportsAsync(request.UserId);

                var dtos = reports.Select(r => new ReportNotificationDTO
                {
                    ReportId = r.Id,
                    Status = (int)r.Status,
                    AdminReply = r.AdminReply,
                    ResolvedAt = r.ResolvedAt
                }).ToList();

                return OperationResult<List<ReportNotificationDTO>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return OperationResult<List<ReportNotificationDTO>>.Failure("Lỗi lấy thông báo.");
            }
        }
    }
}