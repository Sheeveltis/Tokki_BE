using FluentAssertions;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Reports.Commands.UpdateReportStatus;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Reports.Commands
{
    public class UpdateReportStatusHandlerTests : ReportTestBase
    {
        private readonly UpdateReportStatusHandler _handler;

        public UpdateReportStatusHandlerTests()
        {
            _handler = new UpdateReportStatusHandler(_mockReportRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_ReportNotFound()
        {
            var command = ReportTestData.GetUpdateStatusCommand("RP_MISSING", ReportStatus.Processing);
            _mockReportRepo.Setup(x => x.GetByIdAsync(command.ReportId)).ReturnsAsync((Report?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.ReportNotFound.Code);
        }

        [Theory]
        [InlineData(ReportStatus.Fixed)]
        [InlineData(ReportStatus.Rejected)]
        public async Task Handle_Should_SetResolvedAt_And_Unread_When_StatusIsFinal(ReportStatus finalStatus)
        {
            var command = ReportTestData.GetUpdateStatusCommand("RP_01", finalStatus);
            var report = ReportTestData.GetReport("RP_01", "user-01", ReportStatus.Pending);

            _mockReportRepo.Setup(x => x.GetByIdAsync(command.ReportId)).ReturnsAsync(report);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            report.Status.Should().Be(finalStatus);
            report.AdminReply.Should().Be(command.AdminReply);
            report.UserHasRead.Should().BeFalse(); 
            report.ResolvedAt.Should().NotBeNull();
            report.ResolvedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

            _mockReportRepo.Verify(x => x.UpdateAsync(report), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_MarkRead_When_StatusIsProcessing()
        {
            var command = ReportTestData.GetUpdateStatusCommand("RP_01", ReportStatus.Processing);
            var report = ReportTestData.GetReport("RP_01", "user-01", ReportStatus.Pending);

            _mockReportRepo.Setup(x => x.GetByIdAsync(command.ReportId)).ReturnsAsync(report);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            report.Status.Should().Be(ReportStatus.Processing);
            report.UserHasRead.Should().BeTrue(); 

            _mockReportRepo.Verify(x => x.UpdateAsync(report), Times.Once);
        }
    }
}