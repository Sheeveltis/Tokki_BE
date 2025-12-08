using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Reports.Commands.MarkReportRead
{
    public class MarkReportReadHandler : IRequestHandler<MarkReportReadCommand, OperationResult<bool>>
    {
        private readonly IReportRepository _reportRepository;

        public MarkReportReadHandler(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<OperationResult<bool>> Handle(MarkReportReadCommand request, CancellationToken cancellationToken)
        {
            var report = await _reportRepository.GetByIdAsync(request.ReportId);
            if (report == null)
                return OperationResult<bool>.Failure("Không tìm thấy report.");

            if (report.UserId != request.UserId)
                return OperationResult<bool>.Failure("Không có quyền truy cập.");

            report.UserHasRead = true;
            await _reportRepository.UpdateAsync(report);

            return OperationResult<bool>.Success(true);
        }
    }
}