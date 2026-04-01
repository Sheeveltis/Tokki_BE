using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockReportRepository
    {
        public static Mock<IReportRepository> GetMock(
            Report?       returnedById   = null,
            List<Report>? returnedAll    = null,
            List<Report>? returnedUnread = null)
        {
            var mock = new Mock<IReportRepository>();

            mock.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(returnedById);

            mock.Setup(x => x.GetAllAsync(It.IsAny<ReportStatus?>()))
                .ReturnsAsync(returnedAll ?? new List<Report>());

            mock.Setup(x => x.GetByUserIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Report>());

            mock.Setup(x => x.GetUnreadResolvedReportsAsync(It.IsAny<string>()))
                .ReturnsAsync(returnedUnread ?? new List<Report>());

            mock.Setup(x => x.AddAsync(It.IsAny<Report>()))
                .ReturnsAsync((Report r) => r);

            mock.Setup(x => x.UpdateAsync(It.IsAny<Report>()))
                .Returns(Task.CompletedTask);

            mock.Setup(x => x.DeleteAsync(It.IsAny<Report>()))
                .Returns(Task.CompletedTask);

            return mock;
        }

        // ── Sample Data ───────────────────────────────────────────
        public static Report GetSamplePendingReport(string id = "RPT-001", string userId = "USER-001")
            => new Report
            {
                Id          = id,
                UserId      = userId,
                Description = "Sample report",
                ReportType  = "Bug",
                Status      = ReportStatus.Pending,
                UserHasRead = true
            };

        public static Report GetSampleFixedReport(string id = "RPT-002", string userId = "USER-001")
            => new Report
            {
                Id          = id,
                UserId      = userId,
                Description = "Fixed report",
                ReportType  = "Bug",
                Status      = ReportStatus.Fixed,
                UserHasRead = false,
                AdminReply  = "Fixed in v2.1"
            };

        public static Report GetSampleRejectedReport(string id = "RPT-003", string userId = "USER-001")
            => new Report
            {
                Id          = id,
                UserId      = userId,
                Description = "Rejected report",
                ReportType  = "UI",
                Status      = ReportStatus.Rejected,
                UserHasRead = false,
                AdminReply  = "Not a bug"
            };

        public static List<Report> GetSampleReportList(int count = 3, string userId = "USER-001")
        {
            var list = new List<Report>();
            for (int i = 1; i <= count; i++)
                list.Add(GetSamplePendingReport($"RPT-00{i}", userId));
            return list;
        }
    }
}
