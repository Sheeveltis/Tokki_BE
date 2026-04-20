using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Titles.Commands.EquipTitle;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Titles.Commands
{
    public class EquipTitleCommandHandlerTests
    {
        private readonly Mock<IUserTitleService> _mockService;
        private readonly EquipTitleCommandHandler _handler;

        public EquipTitleCommandHandlerTests()
        {
            _mockService = new Mock<IUserTitleService>();
            _handler = new EquipTitleCommandHandler(_mockService.Object);
        }

        [Fact]
        public async Task Handle_EquipFailed_ReturnsFailure()
        {
            _mockService.Setup(x => x.EquipTitleAsync("u1", "t1"))
                        .ReturnsAsync(false);

            var result = await _handler.Handle(new EquipTitleCommand { UserId = "u1", TitleId = "t1" }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be("Bạn chưa sở hữu danh hiệu này hoặc danh hiệu không tồn tại.");

            QACollector.LogTestCase("Title - Equip", new TestCaseDetail
            {
                FunctionGroup     = "EquipTitleCommandHandler",
                TestCaseID        = "EquipTitleCommandHandler_01",
                Description       = "Equip fails due to ownership check",
                ExpectedResult    = "400 Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Returns false from service" }
            });
        }

        [Fact]
        public async Task Handle_EquipSuccess_ReturnsSuccess()
        {
            _mockService.Setup(x => x.EquipTitleAsync("u1", "t1"))
                        .ReturnsAsync(true);

            var result = await _handler.Handle(new EquipTitleCommand { UserId = "u1", TitleId = "t1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Be("Trang bị danh hiệu thành công.");
            result.Data.Should().BeTrue();

            QACollector.LogTestCase("Title - Equip", new TestCaseDetail
            {
                FunctionGroup     = "EquipTitleCommandHandler",
                TestCaseID        = "EquipTitleCommandHandler_02",
                Description       = "Equip succeeds",
                ExpectedResult    = "200 Success",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Returns true from service" }
            });
        }
    }
}
