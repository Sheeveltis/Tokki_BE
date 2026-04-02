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
                    : MockHttpContextAccessor.GetMock("ADMIN-001").Object,
                new Mock<ILogger<DeleteVocabularyExampleCommandHandler>>().Object);
        }

        [Fact]
        public async Task Handle_Unauthorized_ShouldReturn401()
        {
            var command = new DeleteVocabularyExampleCommand { ExampleId = "EX-001" };

            var handler = CreateHandler(unauthorized: true);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            QACollector.LogTestCase("VocabExample - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vocabulary Example",
                TestCaseID = "TC-VEXM-DEL-01",
                Description = "Xóa câu ví dụ khi không có token xác thực",
                ExpectedResult = "Return 401 Unauthorized",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "No UserId in Claims",
                    "Return 401"
                }
            });
        }

        [Fact]
        public async Task Handle_ExampleNotFound_ShouldReturn404()
        {
            var command = new DeleteVocabularyExampleCommand { ExampleId = "EX-INVALID" };

            var handler = CreateHandler(
                exampleRepo: MockVocabularyExampleRepository.GetMock(existingById: null));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("VocabExample - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vocabulary Example",
                TestCaseID = "TC-VEXM-DEL-02",
                Description = "Xóa câu ví dụ với ExampleId không tồn tại",
                ExpectedResult = "Return 404 ExampleNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid ExampleId",
                    "Example = null",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_AlreadyDeleted_ShouldReturnIdempotent200()
        {
            // Example đã Deleted → idempotent, không throw, return 200
            var command = new DeleteVocabularyExampleCommand { ExampleId = "EX-002" };

            var deletedExample = MockVocabularyExampleRepository.GetSampleDeletedExample();

            var handler = CreateHandler(
                exampleRepo: MockVocabularyExampleRepository.GetMock(
                    existingById: deletedExample));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Message.Should().Contain("đã ở trạng thái xóa");

            QACollector.LogTestCase("VocabExample - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vocabulary Example",
                TestCaseID = "TC-VEXM-DEL-03",
                Description = "Xóa câu ví dụ đã ở trạng thái Deleted → idempotent, return 200",
                ExpectedResult = "Return 200, message 'đã ở trạng thái xóa'",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Status = Deleted (boundary: đã xóa)",
                    "Idempotent → return 200"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidActiveExample_ShouldSoftDeleteAndReturn200()
        {
            var command = new DeleteVocabularyExampleCommand { ExampleId = "EX-001" };

            var activeExample = MockVocabularyExampleRepository.GetSampleExample();

            var mockExampleRepo = MockVocabularyExampleRepository.GetMock(
                existingById: activeExample);

            var handler = CreateHandler(exampleRepo: mockExampleRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            activeExample.Status.Should().Be(VocabularyExampleStatus.Deleted);

            // UpdateAsync phải được gọi 1 lần
            mockExampleRepo.Verify(
                x => x.UpdateAsync(It.IsAny<Domain.Entities.VocabularyExample>()),
                Times.Once);

            QACollector.LogTestCase("VocabExample - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vocabulary Example",
                TestCaseID = "TC-VEXM-DEL-04",
                Description = "Xóa câu ví dụ Active hợp lệ → soft delete, Status = Deleted, return 200",
                ExpectedResult = "Status = Deleted, UpdateAsync called once, return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid ExampleId",
                    "Status = Active",
                    "Soft delete → Status = Deleted",
                    "Return 200"
                }
            });
        }
    }
}