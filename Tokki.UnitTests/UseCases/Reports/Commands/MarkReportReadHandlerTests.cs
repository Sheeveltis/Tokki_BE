using FluentAssertions;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Reports.Commands.MarkReportRead;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Reports.Commands
{
    public class MarkReportReadHandlerTests : ReportTestBase
    {
        private readonly MarkReportReadHandler _handler;

        public MarkReportReadHandlerTests()
        {
            _handler = new MarkReportReadHandler(_mockReportRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_MarkingOwnReport()
        {
            var command = new MarkReportReadCommand { ReportId = "RP_01", UserId = "user-01" };
            var report = ReportTestData.GetReport("RP_01", "user-01", ReportStatus.Fixed);
            report.UserHasRead = false;

            _mockReportRepo.Setup(x => x.GetByIdAsync(command.ReportId)).ReturnsAsync(report);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            report.UserHasRead.Should().BeTrue();
            _mockReportRepo.Verify(x => x.UpdateAsync(report), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_MarkingOthersReport()
        {
            var command = new MarkReportReadCommand { ReportId = "RP_01", UserId = "hacker" };
            var report = ReportTestData.GetReport("RP_01", "owner", ReportStatus.Fixed);

            _mockReportRepo.Setup(x => x.GetByIdAsync(command.ReportId)).ReturnsAsync(report);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.ReportUnauthorized.Code);
        }
    }
}