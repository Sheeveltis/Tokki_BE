using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Categories.Commands.CreateCategory;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Categories
{
    public class CreateCategoryCommandHandlerTests
    {
        private CreateCategoryCommandHandler CreateHandler(
            Mock<ICategoryRepository>? repo = null)
        {
            return new CreateCategoryCommandHandler(
                (repo ?? new Mock<ICategoryRepository>()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_EmptyName_ShouldReturn400()
        {
            var command = new CreateCategoryCommand { Name = "" };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Category - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Category",
                TestCaseID = "TC-CAT-CRE-01",
                Description = "Tạo category với Name rỗng",
                ExpectedResult = "Return 400 ValidationFailed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Name = empty string",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidName_ShouldReturn201WithId()
        {
            var command = new CreateCategoryCommand { Name = "Từ vựng cơ bản" };

            var mockRepo = new Mock<ICategoryRepository>();
            mockRepo.Setup(x => x.AddAsync(
                        It.IsAny<Category>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().NotBeNullOrEmpty();

            QACollector.LogTestCase("Category - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Category",
                TestCaseID = "TC-CAT-CRE-02",
                Description = "Tạo category hợp lệ → trả về CategoryId",
                ExpectedResult = "Return 201, Data = CategoryId",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid Name",
                    "AddAsync called",
                    "Return 201"
                }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            var command = new CreateCategoryCommand { Name = "Valid Category" };

            var mockRepo = new Mock<ICategoryRepository>();
            mockRepo.Setup(x => x.AddAsync(
                        It.IsAny<Category>(),
                        It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB error"));

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Category - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Category",
                TestCaseID = "TC-CAT-CRE-03",
                Description = "Repository throw exception → return 500",
                ExpectedResult = "Return 500 ServerError",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "AddAsync throws Exception",
                    "Return 500"
                }
            });
        }
    }
}