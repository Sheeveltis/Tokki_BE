using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Categories.Commands.CreateCategory;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Categories.Commands
{
    public class CreateCategoryCommandHandlerTests
    {
        // ═══════════════════════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════════════════════
        private static CreateCategoryCommandHandler CreateHandler(
            Mock<ICategoryRepository>? repo = null,
            Mock<IIdGeneratorService>? idGen = null)
        {
            repo ??= MockCategoryRepository.GetMock();
            
            if (idGen == null)
            {
                idGen = new Mock<IIdGeneratorService>();
                idGen.Setup(x => x.Generate(It.IsAny<int>())).Returns("CAT-123");
            }

            return new CreateCategoryCommandHandler(repo.Object, idGen.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CC-01 | A | Empty Name → 400 ValidationFailed
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyName_ShouldReturn400()
        {
            var command = new CreateCategoryCommand { Name = "" };
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(AppErrors.ValidationFailed);

            QACollector.LogTestCase("Category - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Category",
                TestCaseID        = "TC-CC-01",
                Description       = "Attempt to create category with empty string name",
                ExpectedResult    = "Return 400 ValidationFailed",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty string", "Returns false success" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CC-02 | B | Whitespace Name → 400 ValidationFailed
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WhitespaceName_ShouldReturn400()
        {
            var command = new CreateCategoryCommand { Name = "   " };
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().Contain(AppErrors.ValidationFailed);

            QACollector.LogTestCase("Category - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Category",
                TestCaseID        = "TC-CC-02",
                Description       = "Attempt to create category with whitespace name",
                ExpectedResult    = "Return 400 ValidationFailed",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Whitespace string", "Null/Whitespace check" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CC-03 | N | Valid Name → 201 Created
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidName_ShouldReturn201Created()
        {
            var mockRepo = MockCategoryRepository.GetMock();
            var command = new CreateCategoryCommand { Name = "Technology" };
            
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("CAT-123");

            // Verify add is called properly
            mockRepo.Verify(x => x.AddAsync(It.Is<Category>(c => c.Name == "Technology"), It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Category - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Category",
                TestCaseID        = "TC-CC-03",
                Description       = "Provide valid category name",
                ExpectedResult    = "Return 201 Created and new Category ID",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid standard name", "Mock AddAsync verified" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CC-04 | N | Verify Slug Generation logic
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidName_ShouldGenerateCorrectSlug()
        {
            var mockRepo = MockCategoryRepository.GetMock();
            var command = new CreateCategoryCommand { Name = "Khoa Học & Đời Sống" };
            
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            mockRepo.Verify(x => x.AddAsync(It.Is<Category>(c => c.Slug == "khoa-hoc-va-doi-song-CAT-123"), It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Category - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Category",
                TestCaseID        = "TC-CC-04",
                Description       = "Provide Vietnamese title to verify Slug normalizer",
                ExpectedResult    = "Slug should strip accents, symbols and map to lowercase format",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Test Regex parsing on accented names" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CC-05 | A | Internal DB Exception → 500 ServerError
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DatabaseThrowsException_ShouldReturn500()
        {
            var mockRepo = MockCategoryRepository.GetMock();
            mockRepo.Setup(x => x.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("DB Down"));

            var command = new CreateCategoryCommand { Name = "Science" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Errors.Should().Contain(AppErrors.ServerError);

            QACollector.LogTestCase("Category - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Category",
                TestCaseID        = "TC-CC-05",
                Description       = "Simulate unhandled database crash during INSERT command",
                ExpectedResult    = "Catch blocks exception and gracefully returns 500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Repository throws Exception" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-CC-06 | N | Check CreatedAt timestamp is populated
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidName_ShouldBindCreatedAt()
        {
            var mockRepo = MockCategoryRepository.GetMock();
            var command = new CreateCategoryCommand { Name = "Games" };
            
            await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            mockRepo.Verify(x => x.AddAsync(It.Is<Category>(c => c.CreatedAt != default), It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Category - Create", new TestCaseDetail
            {
                FunctionGroup     = "Create Category",
                TestCaseID        = "TC-CC-06",
                Description       = "Ensure the created Entity has an active timestamp",
                ExpectedResult    = "CreatedAt struct is instantiated to current UTC Time",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Property validation inside repository pass array" }
            });
        }
    }
}
