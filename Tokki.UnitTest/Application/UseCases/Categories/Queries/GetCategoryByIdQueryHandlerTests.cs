using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Categories.Queries.GetCategoryById;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Categories.Queries
{
    public class GetCategoryByIdQueryHandlerTests
    {
        private readonly Mock<ICategoryRepository> _repoMock = new();

        private GetCategoryByIdQueryHandler CreateHandler()
        {
            return new GetCategoryByIdQueryHandler(_repoMock.Object);
        }

        // GetCategoryByIdQueryHandler_01 | A | Category Not Found
        [Fact]
        public async Task Handle_CategoryNotFound_ShouldReturn404()
        {
            _repoMock.Setup(x => x.GetByIdAsync("fake", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Category?)null);

            var handler = CreateHandler();
            var result = await handler.Handle(new GetCategoryByIdQuery("fake"), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Category - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetCategoryByIdQueryHandler",
                TestCaseID = "GetCategoryByIdQueryHandler_01",
                Description = "Returns 404 cleanly when Category misses",
                ExpectedResult = "404 NotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync -> null" }
            });
        }

        // GetCategoryByIdQueryHandler_02 | N | Entity maps to DTO safely
        [Fact]
        public async Task Handle_CategoryMapped_ShouldReturnCategoryDTO()
        {
            var dtNow = DateTime.UtcNow;
            var category = new Category { Id = "c1", Name = "Name", Slug = "slug", CreatedAt = dtNow };
            _repoMock.Setup(x => x.GetByIdAsync("c1", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(category);

            var handler = CreateHandler();
            var result = await handler.Handle(new GetCategoryByIdQuery("c1"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be("c1");
            result.Data.Slug.Should().Be("slug");
            result.Data.CreatedAt.Should().BeCloseTo(dtNow, TimeSpan.FromSeconds(1));

            QACollector.LogTestCase("Category - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetCategoryByIdQueryHandler",
                TestCaseID = "GetCategoryByIdQueryHandler_02",
                Description = "Valid map populates exact properties down to CreatedAt",
                ExpectedResult = "Success, Properties match natively",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Category returned" }
            });
        }

        // GetCategoryByIdQueryHandler_03 | B | Validation 404 message output format
        [Fact]
        public async Task Handle_CategoryNotFoundMessage_ShouldReadCorrectly()
        {
            _repoMock.Setup(x => x.GetByIdAsync("fake", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Category?)null);

            var handler = CreateHandler();
            var result = await handler.Handle(new GetCategoryByIdQuery("fake"), CancellationToken.None);

            result.Message.Should().Contain("th?t b?i"); // AppErrors uses predefined strings. Typically"L?y Danh m?c th?t b?i."

            QACollector.LogTestCase("Category - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetCategoryByIdQueryHandler",
                TestCaseID = "GetCategoryByIdQueryHandler_03",
                Description = "Expected message uses generic wrapper OperationMessages.GetFail",
                ExpectedResult = "Matches OperationMessages format",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "null return" }
            });
        }

        // GetCategoryByIdQueryHandler_04 | B | Validation 200 message output format
        [Fact]
        public async Task Handle_CategorySuccessMessage_ShouldReadCorrectly()
        {
            var category = new Category { Id = "c1" };
            _repoMock.Setup(x => x.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(category);

            var handler = CreateHandler();
            var result = await handler.Handle(new GetCategoryByIdQuery("c1"), CancellationToken.None);

            result.Message.Should().Contain("thành công");

            QACollector.LogTestCase("Category - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetCategoryByIdQueryHandler",
                TestCaseID = "GetCategoryByIdQueryHandler_04",
                Description = "Expected 200 message uses generic wrapper OperationMessages.GetSuccess",
                ExpectedResult = "Matches OperationMessages format",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "category returned" }
            });
        }

        // GetCategoryByIdQueryHandler_05 | B | Verifies exactly one call to the repo GetByIdAsync
        [Fact]
        public async Task Handle_VerifyCallCountToRepo()
        {
            var category = new Category { Id = "c1" };
            _repoMock.Setup(x => x.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(category);

            var handler = CreateHandler();
            await handler.Handle(new GetCategoryByIdQuery("c1"), CancellationToken.None);

            _repoMock.Verify(x => x.GetByIdAsync("c1", It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Category - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetCategoryByIdQueryHandler",
                TestCaseID = "GetCategoryByIdQueryHandler_05",
                Description = "Validates exactly 1 interaction occurred locally through dependency map",
                ExpectedResult = "Times.Once",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repo hook" }
            });
        }

        // GetCategoryByIdQueryHandler_06 | N | Slug populates seamlessly
        [Fact]
        public async Task Handle_CategoryMappedSlug_ReturnsProperValue()
        {
            var category = new Category { Id = "c1", Name = "X", Slug = "slug-name", CreatedAt = DateTime.UtcNow };
            _repoMock.Setup(x => x.GetByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(category);

            var handler = CreateHandler();
            var result = await handler.Handle(new GetCategoryByIdQuery("c1"), CancellationToken.None);

            result.Data!.Slug.Should().Be("slug-name");

            QACollector.LogTestCase("Category - Get By Id", new TestCaseDetail
            {
                FunctionGroup = "GetCategoryByIdQueryHandler",
                TestCaseID = "GetCategoryByIdQueryHandler_06",
                Description = "Slug DTO map handles populated schema constraints perfectly",
                ExpectedResult = "slug-name",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Slug property populated" }
            });
        }
    }
}
