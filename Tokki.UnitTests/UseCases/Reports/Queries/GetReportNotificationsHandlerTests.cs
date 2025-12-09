using FluentAssertions;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Reports.Queries.GetReportNotifications;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Reports.Queries
{
    public class GetReportNotificationsHandlerTests : ReportTestBase
    {
        private readonly GetReportNotificationsHandler _handler;

        public GetReportNotificationsHandlerTests()
        {
            _handler = new GetReportNotificationsHandler(_mockReportRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnDTOs_When_FoundReports()
        {
            var userId = "user-01";
            var reports = new List<Report>
            {
                ReportTestData.GetReport("RP_01", userId, ReportStatus.Fixed),
                ReportTestData.GetReport("RP_02", userId, ReportStatus.Rejected)
            };

            _mockReportRepo.Setup(x => x.GetUnreadResolvedReportsAsync(userId))
                           .ReturnsAsync(reports);

            var query = new GetReportNotificationsQuery { UserId = userId };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data[0].ReportId.Should().Be("RP_01");
            result.Data[1].Status.Should().Be((int)ReportStatus.Rejected);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmpty_When_NoReports()
        {
            _mockReportRepo.Setup(x => x.GetUnreadResolvedReportsAsync(It.IsAny<string>()))
                           .ReturnsAsync(new List<Report>());

            var query = new GetReportNotificationsQuery { UserId = "user-empty" };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_ExceptionOccurs()
        {
            _mockReportRepo.Setup(x => x.GetUnreadResolvedReportsAsync(It.IsAny<string>()))
                           .ThrowsAsync(new Exception("DB Error"));

            var query = new GetReportNotificationsQuery { UserId = "user-err" };
            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.ReportFetchFailed.Code);
        }
    }
}