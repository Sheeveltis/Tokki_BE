using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Reports.Commands.DeleteReport
{
    public class DeleteReportHandler : IRequestHandler<DeleteReportCommand, OperationResult<bool>>
    {
        private readonly IReportRepository _reportRepository;

        public DeleteReportHandler(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<OperationResult<bool>> Handle(DeleteReportCommand request, CancellationToken cancellationToken)
        {
            var report = await _reportRepository.GetByIdAsync(request.ReportId);
            if (report == null)
                return OperationResult<bool>.Failure("Không tìm thấy report.");

            if (!request.IsAdmin)
            {
                if (report.UserId != request.UserId)
                    return OperationResult<bool>.Failure("Bạn không có quyền xóa report này.");

                if (report.Status != ReportStatus.Pending)
                    return OperationResult<bool>.Failure("Không thể xóa báo cáo đang xử lý hoặc đã xong.");
            }

            await _reportRepository.DeleteAsync(report);
            return OperationResult<bool>.Success(true);
        }
    }
}