using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Categories.Queries;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Categories.Queries
{
    public class GetAllCategoriesQueryHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static GetAllCategoriesQueryHandler CreateHandler(Mock<ICategoryRepository>? repo = null)
        {
            return new GetAllCategoriesQueryHandler((repo ?? MockCategoryRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Get_All_Categories_01 | N | Empty Results → 200 Success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyDatabase_ShouldReturnEmptyList()
        {
            var mockRepo = MockCategoryRepository.GetMock(new List<Category>());
            var query = new GetAllCategoriesQuery();
            
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("Category - Get All", new TestCaseDetail
            {
                FunctionGroup     = "Get All Categories",
                TestCaseID        = "Get_All_Categories_01",
                Description       = "Request to fetch all categories when table is empty",
                ExpectedResult    = "Return 200 Success with zero items in enumerable output",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetAllAsync returns empty list" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_All_Categories_02 | N | Populated List → 200 Success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PopulatedDatabase_ShouldReturnItems()
        {
            var categories = new List<Category>
            {
                MockCategoryRepository.GetSampleCategory("C1"),
                MockCategoryRepository.GetSampleCategory("C2")
            };
            var mockRepo = MockCategoryRepository.GetMock(categories);
            
            var query = new GetAllCategoriesQuery();
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);

            QACollector.LogTestCase("Category - Get All", new TestCaseDetail
            {
                FunctionGroup     = "Get All Categories",
                TestCaseID        = "Get_All_Categories_02",
                Description       = "Fetch query with multiple rows available",
                ExpectedResult    = "Return 200 Success matching expected length array",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Standard mapped response iteration" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_All_Categories_03 | N | DTO Name Mapping
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidQuery_ShouldMapNameAccurately()
        {
            var category = MockCategoryRepository.GetSampleCategory("C1", "Music");
            var mockRepo = MockCategoryRepository.GetMock(new List<Category> { category });
            
            var query = new GetAllCategoriesQuery();
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.Data.First().Name.Should().Be("Music");

            QACollector.LogTestCase("Category - Get All", new TestCaseDetail
            {
                FunctionGroup     = "Get All Categories",
                TestCaseID        = "Get_All_Categories_03",
                Description       = "Verify structural extraction of literal Name properties",
                ExpectedResult    = "Name property is identical to entity state",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Validation of object model fields" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_All_Categories_04 | N | DTO Slug Mapping
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidQuery_ShouldMapSlugAccurately()
        {
            var category = MockCategoryRepository.GetSampleCategory("C1");
            category.Slug = "music-category-slug";
            var mockRepo = MockCategoryRepository.GetMock(new List<Category> { category });
            
            var query = new GetAllCategoriesQuery();
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.Data.First().Slug.Should().Be("music-category-slug");

            QACollector.LogTestCase("Category - Get All", new TestCaseDetail
            {
                FunctionGroup     = "Get All Categories",
                TestCaseID        = "Get_All_Categories_04",
                Description       = "Verify structural extraction of URL-friendly Slugs",
                ExpectedResult    = "Slug strings are accurately maintained throughout domain mapping",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Select projection mapping checks" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_All_Categories_05 | A | Internal Repo Exception → Throws Or 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DatabaseException_ShouldBubbleUp()
        {
            var mockRepo = MockCategoryRepository.GetMock();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Database connection failure"));
            
            var query = new GetAllCategoriesQuery();
            var handler = CreateHandler(mockRepo);

            // Handler doesn't use try catch block for queries here, so assert ThrowsAsync
            await Assert.ThrowsAsync<Exception>(async () => await handler.Handle(query, CancellationToken.None));

            QACollector.LogTestCase("Category - Get All", new TestCaseDetail
            {
                FunctionGroup     = "Get All Categories",
                TestCaseID        = "Get_All_Categories_05",
                Description       = "Database driver drops connection during list enumeration",
                ExpectedResult    = "Exception bubbles up directly through pipeline for global logging",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Global exception handler fallback" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Get_All_Categories_06 | N | DTO CreatedAt Mapping
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidQuery_ShouldMapCreatedAtProperty()
        {
            var staticTime = new DateTimeOffset(2025, 01, 01, 12, 0, 0, TimeSpan.Zero);
            var category = MockCategoryRepository.GetSampleCategory("C1");
            category.CreatedAt = staticTime;
            
            var mockRepo = MockCategoryRepository.GetMock(new List<Category> { category });
            
            var query = new GetAllCategoriesQuery();
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.Data.First().CreatedAt.Should().Be(staticTime);

            QACollector.LogTestCase("Category - Get All", new TestCaseDetail
            {
                FunctionGroup     = "Get All Categories",
                TestCaseID        = "Get_All_Categories_06",
                Description       = "Verifies standard date projection binding on CategoryDTOs",
                ExpectedResult    = "DTO populates CreatedAt corresponding perfectly to Entity memory",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "DateTime projection tracking logic" }
            });
        }
    }
}
