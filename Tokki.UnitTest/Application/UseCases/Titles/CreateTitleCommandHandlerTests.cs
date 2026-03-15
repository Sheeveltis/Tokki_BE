using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Titles.Commands.CreateTitle;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Titles
{
    public class CreateTitleCommandHandlerTests
    {
        private CreateTitleCommandHandler CreateHandler(
            Mock<ITitleRepository>? repo = null)
        {
            return new CreateTitleCommandHandler(
                (repo ?? new Mock<ITitleRepository>()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_DuplicateName_ShouldReturn400()
        {
            var command = new CreateTitleCommand
            {
                Name = "Học Giả",
                RequiredXP = 100
            };

            var mockRepo = new Mock<ITitleRepository>();
            mockRepo.Setup(x => x.GetTitleByNameAsync("Học Giả"))
                    .ReturnsAsync(new Title { TitleId = "TITLE-001", Name = "Học Giả" });

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("đã tồn tại");

            QACollector.LogTestCase("Title - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Title",
                TestCaseID = "TC-TTL-CRE-01",
                Description = "Tạo danh hiệu với tên đã tồn tại",
                ExpectedResult = "Return 400 với message 'đã tồn tại'",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Title Name đã tồn tại",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_NegativeXP_ShouldReturn400()
        {
            var command = new CreateTitleCommand
            {
                Name = "New Title",
                RequiredXP = -10 // XP âm
            };

            var mockRepo = new Mock<ITitleRepository>();
            mockRepo.Setup(x => x.GetTitleByNameAsync(It.IsAny<string>()))
                    .ReturnsAsync((Title?)null); // name chưa tồn tại

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("âm");

            QACollector.LogTestCase("Title - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Title",
                TestCaseID = "TC-TTL-CRE-02",
                Description = "Tạo danh hiệu với RequiredXP âm",
                ExpectedResult = "Return 400 với message 'không được âm'",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "RequiredXP = -10 (invalid)",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidData_ShouldReturn201WithTitle()
        {
            var command = new CreateTitleCommand
            {
                Name = "Học Giả Mới",
                RequiredXP = 500,
                Description = "Danh hiệu cho người học chăm chỉ"
            };

            var mockRepo = new Mock<ITitleRepository>();
            mockRepo.Setup(x => x.GetTitleByNameAsync(It.IsAny<string>()))
                    .ReturnsAsync((Title?)null);
            mockRepo.Setup(x => x.AddAsync(It.IsAny<Title>()))
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Name.Should().Be("Học Giả Mới");

            QACollector.LogTestCase("Title - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Title",
                TestCaseID = "TC-TTL-CRE-03",
                Description = "Tạo danh hiệu hợp lệ → trả về Title object",
                ExpectedResult = "Return 201, Data.Name = 'Học Giả Mới'",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid Name, RequiredXP >= 0",
                    "AddAsync called",
                    "Return 201"
                }
            });
        }
    }
}