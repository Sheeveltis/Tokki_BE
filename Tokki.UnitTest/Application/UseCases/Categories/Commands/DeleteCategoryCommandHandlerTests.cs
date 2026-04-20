using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Categories.Commands.DeleteCategory;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Categories.Commands
{
    public class DeleteCategoryCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static DeleteCategoryCommandHandler CreateHandler(Mock<ICategoryRepository>? repo = null)
        {
            return new DeleteCategoryCommandHandler((repo ?? MockCategoryRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // Delete_Category_01 | A | CategoryNotFound → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_CategoryNotFound_ShouldReturn404()
        {
            var command = new DeleteCategoryCommand { Id = "MISSING" };
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().Contain(AppErrors.CategoryNotFound);

            QACollector.LogTestCase("Category - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Category",
                TestCaseID        = "Delete_Category_01",
                Description       = "Attempt to delete a category that does not exist",
                ExpectedResult    = "Return 404 NotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Delete_Category_02 | A | Database Exception → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DatabaseException_ShouldReturn500()
        {
            var category = MockCategoryRepository.GetSampleCategory();
            var mockRepo = MockCategoryRepository.GetMock(new List<Category> { category });
            
            mockRepo.Setup(x => x.DeleteAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB Offline"));

            var command = new DeleteCategoryCommand { Id = "CAT-001" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Errors.Should().Contain(AppErrors.ServerError);

            QACollector.LogTestCase("Category - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Category",
                TestCaseID        = "Delete_Category_02",
                Description       = "Exception occurs during repository DeleteAsync",
                ExpectedResult    = "Return 500 ServerError",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Simulate Exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Delete_Category_03 | N | Valid Delete → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn200()
        {
            var category = MockCategoryRepository.GetSampleCategory();
            var mockRepo = MockCategoryRepository.GetMock(new List<Category> { category });

            var command = new DeleteCategoryCommand { Id = "CAT-001" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Verify
            mockRepo.Verify(x => x.DeleteAsync(It.Is<Category>(c => c.Id == "CAT-001"), It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Category - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Category",
                TestCaseID        = "Delete_Category_03",
                Description       = "Valid payload matching existing Category",
                ExpectedResult    = "Return 200 Success and executes delete",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Data passes successfully" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Delete_Category_04 | B | Null ID → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NullId_ShouldReturn404()
        {
            var command = new DeleteCategoryCommand { Id = null! };
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Category - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Category",
                TestCaseID        = "Delete_Category_04",
                Description       = "Delete payload is sent with null ID",
                ExpectedResult    = "Lookup fails, returns 404 safely",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Boundary logic check" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Delete_Category_05 | N | Verify Reference Matching
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Delete_VerifiesEntityReference()
        {
            var category = MockCategoryRepository.GetSampleCategory("REF-1");
            var mockRepo = MockCategoryRepository.GetMock(new List<Category> { category });

            var command = new DeleteCategoryCommand { Id = "REF-1" };
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            mockRepo.Verify(x => x.DeleteAsync(category, It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Category - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Category",
                TestCaseID        = "Delete_Category_05",
                Description       = "Ensure the exact object instance retrieved is passed into Delete",
                ExpectedResult    = "DeleteAsync verifies the same reference",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Verify EF tracking consistency" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Delete_Category_06 | N | Localized Error Handling
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Exception_ValidatesMessageString()
        {
            var category = MockCategoryRepository.GetSampleCategory();
            var mockRepo = MockCategoryRepository.GetMock(new List<Category> { category });
            mockRepo.Setup(x => x.DeleteAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Any Exception"));

            var command = new DeleteCategoryCommand { Id = "CAT-001" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.Message.Should().Contain("Không thể xóa Danh mục");

            QACollector.LogTestCase("Category - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Category",
                TestCaseID        = "Delete_Category_06",
                Description       = "Database constraints or unexpected failures",
                ExpectedResult    = "Return generic localized error message",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Test localized strings on exception" }
            });
        }
    }
}
