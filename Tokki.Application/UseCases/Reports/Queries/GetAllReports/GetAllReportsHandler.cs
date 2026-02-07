using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Reports.DTOs;

namespace Tokki.Application.UseCases.Reports.Queries.GetAllReports
{
    public class GetAllReportsHandler : IRequestHandler<GetAllReportsQuery, OperationResult<List<ReportNotificationDTO>>>
    {
        private readonly IReportRepository _reportRepository;

        public GetAllReportsHandler(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<OperationResult<List<ReportNotificationDTO>>> Handle(GetAllReportsQuery request, CancellationToken cancellationToken)
        {
            var reports = await _reportRepository.GetAllAsync(request.Status);

            var dtos = reports.Select(r => new ReportNotificationDTO
            {
                ReportId = r.Id,
                Status = (int)r.Status,
                AdminReply = r.AdminReply,
                ResolvedAt = r.ResolvedAt,
                Description = r.Description,
                TargetUrl = r.TargetUrl,
                ImageUrl = r.ImageUrl
            }).ToList();

            return OperationResult<List<ReportNotificationDTO>>.Success(dtos);
        }
    }
}