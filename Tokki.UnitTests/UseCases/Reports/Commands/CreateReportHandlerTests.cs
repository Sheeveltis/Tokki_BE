using FluentAssertions;
using Moq;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Reports.Commands.CreateReport;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTests.Common.Bases;
using Tokki.UnitTests.Common.TestData;
using Xunit;

namespace Tokki.UnitTests.Features.Reports.Commands
{
    public class CreateReportHandlerTests : ReportTestBase
    {
        private readonly CreateReportHandler _handler;

        public CreateReportHandlerTests()
        {
            _handler = new CreateReportHandler(_mockReportRepo.Object, _mockIdGen.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_InputIsValid()
        {
            var command = ReportTestData.GetCreateCommand();

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("REPORT_MOCK_ID");

            _mockReportRepo.Verify(x => x.AddAsync(It.Is<Report>(r =>
                r.Id == "REPORT_MOCK_ID" &&
                r.UserId == command.UserId &&
                r.Status == ReportStatus.Pending &&
                r.Description == command.Description
            )), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RepositoryThrowsException()
        {
            var command = ReportTestData.GetCreateCommand();
            _mockReportRepo.Setup(x => x.AddAsync(It.IsAny<Report>()))
                           .ThrowsAsync(new Exception("DB Error"));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == AppErrors.ReportCreationFailed.Code);
        }
    }
}