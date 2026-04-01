using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionTypes.Queries.GetQuestionTypeById;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionTypes
{
    public class GetQuestionTypeByIdQueryHandlerTests
    {
        private static GetQuestionTypeByIdQueryHandler CreateHandler(
            Mock<IQuestionTypeRepository>? repo = null)
        {
            return new GetQuestionTypeByIdQueryHandler(
                (repo ?? MockQuestionTypeRepository.GetMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QT-GBI-01 | A | Empty Id → failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyId_ShouldReturnFailure()
        {
            // Arrange
            var handler = CreateHandler();
            var query   = new GetQuestionTypeByIdQuery("");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("ID");

            QACollector.LogTestCase("Question Type - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionTypeById",
                TestCaseID        = "TC-QT-GBI-01",
                Description       = "Empty Id → failure with 'ID không hợp lệ' message",
                ExpectedResult    = "IsSuccess=false, Message contains 'ID'",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Id = empty string", "failure returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QT-GBI-02 | A | Whitespace Id → failure
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WhitespaceId_ShouldReturnFailure()
        {
            // Arrange — IsNullOrEmpty("   ") = false, but handler uses IsNullOrEmpty not IsNullOrWhiteSpace
            // Whitespace passes the guard → returns 404 from repo instead
            var repo    = MockQuestionTypeRepository.GetMock(returnedById: null);
            var handler = CreateHandler(repo);
            var query   = new GetQuestionTypeByIdQuery("   ");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert — whitespace isn't empty so goes to repo → null → 404
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("Question Type - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionTypeById",
                TestCaseID        = "TC-QT-GBI-02",
                Description       = "Whitespace Id passes IsNullOrEmpty → reaches repo → null → failure",
                ExpectedResult    = "IsSuccess=false",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Id = whitespace", "reaches repo, returns null", "failure" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QT-GBI-03 | A | Valid Id but entity not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidIdButNotFound_ShouldReturn404()
        {
            // Arrange
            var repo    = MockQuestionTypeRepository.GetMock(returnedById: null);
            var handler = CreateHandler(repo);
            var query   = new GetQuestionTypeByIdQuery("QT-MISSING");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Type - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionTypeById",
                TestCaseID        = "TC-QT-GBI-03",
                Description       = "Valid Id but entity not found → 404 failure",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Id non-empty", "GetByIdAsync returns null", "404" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QT-GBI-04 | N | Happy path → entity returned, success
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidIdExists_ShouldReturnEntityAndSuccess()
        {
            // Arrange
            var qt      = MockQuestionTypeRepository.GetSampleActive("QT-001", QuestionSkill.Listening);
            var repo    = MockQuestionTypeRepository.GetMock(returnedById: qt);
            var handler = CreateHandler(repo);
            var query   = new GetQuestionTypeByIdQuery("QT-001");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.QuestionTypeId.Should().Be("QT-001");
            result.Data.Skill.Should().Be(QuestionSkill.Listening);

            QACollector.LogTestCase("Question Type - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionTypeById",
                TestCaseID        = "TC-QT-GBI-04",
                Description       = "Happy path: entity found and returned in Data, success",
                ExpectedResult    = "IsSuccess=true, Data.QuestionTypeId='QT-001', Data.Skill=Listening",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns entity", "success" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QT-GBI-05 | N | Inactive entity → still returned (no IsActive filter)
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_InactiveEntity_ShouldStillReturnIt()
        {
            // Arrange
            var qt      = MockQuestionTypeRepository.GetSampleInactive("QT-INV-01");
            var repo    = MockQuestionTypeRepository.GetMock(returnedById: qt);
            var handler = CreateHandler(repo);
            var query   = new GetQuestionTypeByIdQuery("QT-INV-01");

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.IsActive.Should().BeFalse();

            QACollector.LogTestCase("Question Type - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionTypeById",
                TestCaseID        = "TC-QT-GBI-05",
                Description       = "Inactive entity returned (no IsActive filter in handler), Data.IsActive=false",
                ExpectedResult    = "IsSuccess=true, Data.IsActive=false",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Entity.IsActive=false", "no filter in handler", "success" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QT-GBI-06 | B | GetByIdAsync called exactly once with the correct Id
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidId_GetByIdCalledOnceWithCorrectId()
        {
            // Arrange
            const string targetId = "QT-TARGET-01";
            var qt      = MockQuestionTypeRepository.GetSampleActive(targetId);
            var repo    = MockQuestionTypeRepository.GetMock(returnedById: qt);
            var handler = CreateHandler(repo);
            var query   = new GetQuestionTypeByIdQuery(targetId);

            // Act
            await handler.Handle(query, CancellationToken.None);

            // Assert
            repo.Verify(x => x.GetByIdAsync(targetId, It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Type - Get By Id", new TestCaseDetail
            {
                FunctionGroup     = "GetQuestionTypeById",
                TestCaseID        = "TC-QT-GBI-06",
                Description       = "Boundary: GetByIdAsync called exactly once with the correct target Id",
                ExpectedResult    = "GetByIdAsync('QT-TARGET-01') Times.Once",
                StatusRound1      = "Passed",
                TestCaseType      = "B",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Specific id in query", "repo called once with that id" }
            });
        }
    }
}
