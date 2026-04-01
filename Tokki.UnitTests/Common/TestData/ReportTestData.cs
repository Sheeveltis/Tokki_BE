using Tokki.Application.UseCases.Reports.Commands.CreateReport;
using Tokki.Application.UseCases.Reports.Commands.DeleteReport;
using Tokki.Application.UseCases.Reports.Commands.UpdateReportStatus;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    public static class ReportTestData
    {
        public static CreateReportCommand GetCreateCommand()
        {
            return new CreateReportCommand
            {
                UserId = "user-test-01",
                Description = "Image display error",
                ImageUrl = "https://img.com/err.png",
                TargetUrl = "https://tokki.com/post/1"
            };
        }

        public static DeleteReportCommand GetDeleteCommand(string reportId, string userId, bool isAdmin)
        {
            return new DeleteReportCommand
            {
                ReportId = reportId,
                UserId = userId,
                IsAdmin = isAdmin
            };
        }

        public static UpdateReportStatusCommand GetUpdateStatusCommand(string reportId, ReportStatus newStatus)
        {
            return new UpdateReportStatusCommand
            {
                ReportId = reportId,
                NewStatus = newStatus,
                AdminReply = "Processed"
            };
        }

        public static Report GetReport(string id, string userId, ReportStatus status)
        {
            return new Report
            {
                Id = id,
                UserId = userId,
                Status = status,
                Description = "Test Report Description",
                CreatedAt = DateTime.UtcNow,
                UserHasRead = true,
                IsDeleted = false
            };
        }
    }
}