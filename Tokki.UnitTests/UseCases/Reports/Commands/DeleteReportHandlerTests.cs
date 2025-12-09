using FluentAssertions;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Reports.Commands.DeleteReport;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Reports.Commands
{
    public class DeleteReportHandlerTests : ReportTestBase
    {
        private readonly DeleteReportHandler _handler;

        public DeleteReportHandlerTests()
        {
            _handler = new DeleteReportHandler(_mockReportRepo.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_ReportNotFound()
        {
            var command = ReportTestData.GetDeleteCommand("RP_UNKNOWN", "user-01", false);
            _mockReportRepo.Setup(x => x.GetByIdAsync(command.ReportId)).ReturnsAsync((Report?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.ReportNotFound.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserIsNotOwner_And_NotAdmin()
        {
            var command = ReportTestData.GetDeleteCommand("RP_01", "user-hacker", false);
            var report = ReportTestData.GetReport("RP_01", "user-owner", ReportStatus.Pending);

            _mockReportRepo.Setup(x => x.GetByIdAsync(command.ReportId)).ReturnsAsync(report);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.ReportUnauthorized.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserDeletesNonPendingReport()
        {
            var command = ReportTestData.GetDeleteCommand("RP_01", "user-owner", false);
            var report = ReportTestData.GetReport("RP_01", "user-owner", ReportStatus.Fixed);

            _mockReportRepo.Setup(x => x.GetByIdAsync(command.ReportId)).ReturnsAsync(report);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.ReportCannotDelete.Code);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_UserDeletesPendingReport()
        {
            var command = ReportTestData.GetDeleteCommand("RP_01", "user-owner", false);
            var report = ReportTestData.GetReport("RP_01", "user-owner", ReportStatus.Pending);

            _mockReportRepo.Setup(x => x.GetByIdAsync(command.ReportId)).ReturnsAsync(report);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _mockReportRepo.Verify(x => x.DeleteAsync(report), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_AdminDeletesAnyReport()
        {
            var command = ReportTestData.GetDeleteCommand("RP_01", "admin-user", true);
            var report = ReportTestData.GetReport("RP_01", "other-user", ReportStatus.Fixed);

            _mockReportRepo.Setup(x => x.GetByIdAsync(command.ReportId)).ReturnsAsync(report);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _mockReportRepo.Verify(x => x.DeleteAsync(report), Times.Once);
        }
    }
}