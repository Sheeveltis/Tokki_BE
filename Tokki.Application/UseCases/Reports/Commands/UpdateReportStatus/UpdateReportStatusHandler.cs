using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Reports.Commands.UpdateReportStatus
{
    public class UpdateReportStatusHandler : IRequestHandler<UpdateReportStatusCommand, OperationResult<bool>>
    {
        private readonly IReportRepository _reportRepository;

        public UpdateReportStatusHandler(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<OperationResult<bool>> Handle(UpdateReportStatusCommand request, CancellationToken cancellationToken)
        {
            var report = await _reportRepository.GetByIdAsync(request.ReportId);

            if (report == null)
            {
                return OperationResult<bool>.Failure(AppErrors.ReportNotFound);
            }

            report.Status = request.NewStatus;
            report.AdminReply = request.AdminReply;

            if (request.NewStatus == ReportStatus.Fixed || request.NewStatus == ReportStatus.Rejected)
            {
                report.ResolvedAt = DateTime.UtcNow;
                report.UserHasRead = false;
            }
            else
            {
                report.UserHasRead = true;
            }

            await _reportRepository.UpdateAsync(report);

            return OperationResult<bool>.Success(true);
        }
    }
}