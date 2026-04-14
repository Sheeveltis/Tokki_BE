using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Categories.Commands.UpdateCategory;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Categories.Commands
{
    public class UpdateCategoryCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static UpdateCategoryCommandHandler CreateHandler(Mock<ICategoryRepository>? repo = null)
        {
            return new UpdateCategoryCommandHandler((repo ?? MockCategoryRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-UC-01 | A | CategoryNotFound → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CategoryNotFound_ShouldReturn404()
        {
            var command = new UpdateCategoryCommand { Id = "GHOST-ID" };
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.CategoryNotFound);

            QACollector.LogTestCase("Category - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Category",
                TestCaseID        = "TC-UC-01",
                Description       = "Attempt to update a non-existent category",
                ExpectedResult    = "Return 404 CategoryNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-UC-02 | A | Database Exception → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DatabaseException_ShouldReturn500()
        {
            var category = MockCategoryRepository.GetSampleCategory();
            var mockRepo = MockCategoryRepository.GetMock(new List<Category> { category });
            
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB Connection Lost"));

            var command = new UpdateCategoryCommand { Id = "CAT-001", Name = "New Name" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Errors.Should().Contain(AppErrors.ServerError);

            QACollector.LogTestCase("Category - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Category",
                TestCaseID        = "TC-UC-02",
                Description       = "Exception occurs during repository UpdateAsync",
                ExpectedResult    = "Gracefully caught and returns 500 ServerError",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Throw simulated exception on Save" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-UC-03 | N | Valid Update → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn200()
        {
            var category = MockCategoryRepository.GetSampleCategory();
            var mockRepo = MockCategoryRepository.GetMock(new List<Category> { category });

            var command = new UpdateCategoryCommand { Id = "CAT-001", Name = "Updated Category" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Using It.Is matches reference directly inside Moq context
            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Category - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Category",
                TestCaseID        = "TC-UC-03",
                Description       = "Valid payload matching existing Category",
                ExpectedResult    = "Return 200 Success and saves modifications",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Data passes successfully" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-UC-04 | B | Null ID mapped to NotFound Logic  → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullId_ShouldReturn404()
        {
            var command = new UpdateCategoryCommand { Id = null! };
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Category - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Category",
                TestCaseID        = "TC-UC-04",
                Description       = "Update payload is sent with mapped null Identifier",
                ExpectedResult    = "Lookup fails, returns 404",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Boundary logic check for uninitialized structs fallback" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-UC-05 | N | Verify Slug Recalculation
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidUpdate_ShouldRecalculateSlug()
        {
            var category = MockCategoryRepository.GetSampleCategory();
            var mockRepo = MockCategoryRepository.GetMock(new List<Category> { category });

            var command = new UpdateCategoryCommand { Id = "CAT-001", Name = "Nghệ Thuật Mới" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            category.Slug.Should().Be("nghe-thuat-moi-CAT-001");

            QACollector.LogTestCase("Category - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Category",
                TestCaseID        = "TC-UC-05",
                Description       = "Provide new name containing extended characters",
                ExpectedResult    = "Slug should be regenerated referencing the new title",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Trigger Slug string normalization" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-UC-06 | N | Mutates correct pointer verification
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Update_ShouldModifyOriginalEntity()
        {
            var category = MockCategoryRepository.GetSampleCategory();
            var mockRepo = MockCategoryRepository.GetMock(new List<Category> { category });

            var command = new UpdateCategoryCommand { Id = "CAT-001", Name = "Renamed Again" };
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            category.Name.Should().Be("Renamed Again");

            QACollector.LogTestCase("Category - Update", new TestCaseDetail
            {
                FunctionGroup     = "Update Category",
                TestCaseID        = "TC-UC-06",
                Description       = "Verifies standard object tracking pointer manipulation",
                ExpectedResult    = "Original reference updates directly in memory before saving",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "EF Core tracking simulation valid execution" }
            });
        }
    }
}
