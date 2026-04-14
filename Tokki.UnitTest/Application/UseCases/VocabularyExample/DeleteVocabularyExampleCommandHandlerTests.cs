using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VocabularyExample.Commands.DeleteExample;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.VocabularyExample
{
    public class DeleteVocabularyExampleCommandHandlerTests
    {
        private DeleteVocabularyExampleCommandHandler CreateHandler(
            Mock<IVocabularyExampleRepository>? exampleRepo = null,
            bool unauthorized = false)
        {
            return new DeleteVocabularyExampleCommandHandler(
                (exampleRepo ?? MockVocabularyExampleRepository.GetMock()).Object,
                unauthorized
                    ? MockHttpContextAccessor.GetUnauthorizedMock().Object
                    : MockHttpContextAccessor.GetMock("USER-001").Object,
                new Mock<ILogger<DeleteVocabularyExampleCommandHandler>>().Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-DEL-01 | A | No token → 401
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            // Arrange
            var command = new DeleteVocabularyExampleCommand { ExampleId = "EX-001" };
            var handler = CreateHandler(unauthorized: true);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Vocabulary Example",
                TestCaseID        = "TC-VEXM-DEL-01",
                Description       = "Delete example sentence without authentication token",
                ExpectedResult    = "Return 401 Unauthorized",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No UserId in Claims", "Return 401" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-DEL-02 | A | Empty ExampleId → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyExampleId_ShouldReturn400()
        {
            // Arrange
            var command = new DeleteVocabularyExampleCommand { ExampleId = "" };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Vocabulary Example",
                TestCaseID        = "TC-VEXM-DEL-02",
                Description       = "Delete example with empty ExampleId",
                ExpectedResult    = "Return 400 ExampleIdEmpty",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ExampleId = empty string", "Return 400" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-DEL-03 | A | Example not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExampleNotFound_ShouldReturn404()
        {
            // Arrange
            var command = new DeleteVocabularyExampleCommand { ExampleId = "EX-INVALID" };
            var handler = CreateHandler(
                exampleRepo: MockVocabularyExampleRepository.GetMock(existingById: null));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Vocabulary Example",
                TestCaseID        = "TC-VEXM-DEL-03",
                Description       = "Delete example with non-existent ExampleId",
                ExpectedResult    = "Return 404 ExampleNotFound",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invalid ExampleId", "Example = null", "Return 404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-DEL-04 | N | Example already Deleted → 200 (idempotent)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ExampleAlreadyDeleted_ShouldReturn200Idempotent()
        {
            // Arrange
            var command = new DeleteVocabularyExampleCommand { ExampleId = "EX-001" };
            var deletedExample = MockVocabularyExampleRepository.GetSampleExample();
            deletedExample.Status = VocabularyExampleStatus.Deleted;

            var handler = CreateHandler(
                exampleRepo: MockVocabularyExampleRepository.GetMock(existingById: deletedExample));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Vocabulary Example",
                TestCaseID        = "TC-VEXM-DEL-04",
                Description       = "Delete example that is already Deleted → idempotent, returns 200",
                ExpectedResult    = "Return 200 (already deleted, no change needed)",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Status = Deleted (already)", "Idempotent → 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-DEL-05 | N | Valid Active example → soft delete → 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidActiveExample_ShouldSoftDeleteAndReturn200()
        {
            // Arrange
            var command = new DeleteVocabularyExampleCommand { ExampleId = "EX-001" };
            var example = MockVocabularyExampleRepository.GetSampleExample();
            example.Status = VocabularyExampleStatus.Active;

            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(existingById: example);
            var handler = CreateHandler(exampleRepo: mockExampleRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            example.Status.Should().Be(VocabularyExampleStatus.Deleted);
            mockExampleRepo.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.VocabularyExample>()), Times.Once);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Vocabulary Example",
                TestCaseID        = "TC-VEXM-DEL-05",
                Description       = "Delete valid Active example → Status = Deleted, UpdateAsync called",
                ExpectedResult    = "Return 200, Status = Deleted, UpdateAsync called once",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid ExampleId", "Status = Active → Deleted", "UpdateAsync called", "Return 200" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-VEXM-DEL-06 | A | Repository throws → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            // Arrange
            var command = new DeleteVocabularyExampleCommand { ExampleId = "EX-001" };
            var example = MockVocabularyExampleRepository.GetSampleExample();
            example.Status = VocabularyExampleStatus.Active;

            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(existingById: example);
            mockExampleRepo.Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.VocabularyExample>()))
                           .ThrowsAsync(new Exception("DB update failed"));

            var handler = CreateHandler(exampleRepo: mockExampleRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            // Excel Log
            QACollector.LogTestCase("Vocabulary Example - Delete", new TestCaseDetail
            {
                FunctionGroup     = "Delete Vocabulary Example",
                TestCaseID        = "TC-VEXM-DEL-06",
                Description       = "Repository.UpdateAsync throws exception → return 500",
                ExpectedResult    = "Return 500 Server Error",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UpdateAsync throws DB exception", "Return 500" }
            });
        }
    }
}