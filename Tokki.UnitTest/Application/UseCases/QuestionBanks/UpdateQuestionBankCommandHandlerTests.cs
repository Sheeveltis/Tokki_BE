using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks
{
    public class UpdateQuestionBankCommandHandlerTests
    {
        private static Mock<IQuestionTypeRepository> GetActiveQtRepo(QuestionSkill skill = QuestionSkill.Reading)
        {
            var mock = new Mock<IQuestionTypeRepository>();
            mock.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QuestionType
                {
                    QuestionTypeId = "QT-001",
                    Skill          = skill,
                    IsActive       = true
                });
            return mock;
        }

        private static Mock<IQuestionOptionRepository> GetOptionRepoMock()
        {
            var mock = new Mock<IQuestionOptionRepository>();
            mock.Setup(x => x.DeleteByQuestionBankIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mock.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionOption>>()))
                .Returns(Task.CompletedTask);
            return mock;
        }

        private static UpdateQuestionBankCommandHandler CreateHandler(
            Mock<IQuestionBankRepository>?  qbRepo      = null,
            Mock<IQuestionOptionRepository>? optRepo     = null,
            Mock<IQuestionTypeRepository>?   qtRepo      = null,
            Mock<IPassageRepository>?        passageRepo = null,
            Mock<IIdGeneratorService>?       idGen       = null)
        {
            var idGenSvc = idGen ?? new Mock<IIdGeneratorService>();
            idGenSvc.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("OPT-GEN-001");

            return new UpdateQuestionBankCommandHandler(
                (qbRepo      ?? MockQuestionBankRepository.GetMock()).Object,
                (optRepo     ?? GetOptionRepoMock()).Object,
                (qtRepo      ?? GetActiveQtRepo()).Object,
                (passageRepo ?? new Mock<IPassageRepository>()).Object,
                idGenSvc.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // UpdateQuestionBank_01 | A | Empty QuestionBankId → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyQuestionBankId_ShouldReturn400()
        {
            // Arrange
            var handler = CreateHandler();
            var command = new UpdateQuestionBankCommand { QuestionBankId = "" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionBank",
                TestCaseID        = "UpdateQuestionBank_01",
                Description       = "Empty QuestionBankId → 400 ValidationFailed",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionBankId = empty string", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // UpdateQuestionBank_02 | A | QB not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_QBNotFound_ShouldReturn404()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: null);
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new UpdateQuestionBankCommand { QuestionBankId = "QB-MISSING", QuestionTypeId = "QT-001" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionBank",
                TestCaseID        = "UpdateQuestionBank_02",
                Description       = "QuestionBankId not found → 404 QuestionBankNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdWithDetailsAsync returns null", "404 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // UpdateQuestionBank_03 | A | Assigned QB → 403 Forbidden
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_AssignedQB_ShouldReturn403()
        {
            // Arrange
            var assignedQb = new QuestionBank
            {
                QuestionBankId  = "QB-ASGN-01",
                Status          = QuestionBankStatus.Assigned,
                QuestionTypeId  = "QT-001",
                QuestionOptions = new List<QuestionOption>()
            };
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: assignedQb);
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new UpdateQuestionBankCommand
            {
                QuestionBankId = "QB-ASGN-01",
                QuestionTypeId = "QT-001"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionBank",
                TestCaseID        = "UpdateQuestionBank_03",
                Description       = "QB is Assigned → cannot update → 403 Forbidden",
                ExpectedResult    = "IsSuccess=false, StatusCode=403",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Assigned", "403 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // UpdateQuestionBank_04 | A | Deleted QB → 403 Forbidden
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DeletedQB_ShouldReturn403()
        {
            // Arrange
            var deletedQb = MockQuestionBankRepository.GetSampleDeletedQB();
            deletedQb.QuestionTypeId = "QT-001";
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: deletedQb);
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new UpdateQuestionBankCommand
            {
                QuestionBankId = deletedQb.QuestionBankId,
                QuestionTypeId = "QT-001"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionBank",
                TestCaseID        = "UpdateQuestionBank_04",
                Description       = "QB is Deleted → cannot update → 403 Forbidden",
                ExpectedResult    = "IsSuccess=false, StatusCode=403",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Deleted", "403 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // UpdateQuestionBank_05 | A | Listening skill but no MediaUrl after patch → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ListeningWithoutMediaUrl_ShouldReturn400()
        {
            // Arrange
            var draftQb = MockQuestionBankRepository.GetSampleDraftQB();
            draftQb.QuestionTypeId = "QT-001";
            draftQb.MediaUrl = null; // no existing media
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: draftQb);
            var qtRepo  = GetActiveQtRepo(QuestionSkill.Listening);
            var handler = CreateHandler(qbRepo: qbRepo, qtRepo: qtRepo);
            var command = new UpdateQuestionBankCommand
            {
                QuestionBankId = draftQb.QuestionBankId,
                QuestionTypeId = "QT-001",
                MediaUrl       = null // still empty
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionBank",
                TestCaseID        = "UpdateQuestionBank_05",
                Description       = "Listening skill without MediaUrl after patch → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill=Listening", "MediaUrl still null after patch", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // UpdateQuestionBank_06 | N | Happy path: Draft QB update content, 200
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DraftQBUpdateContent_ShouldReturn200()
        {
            // Arrange
            var draftQb = MockQuestionBankRepository.GetSampleDraftQB();
            draftQb.QuestionTypeId = "QT-001";
            draftQb.Content        = "Old content";
            draftQb.QuestionType   = new QuestionType { QuestionTypeId = "QT-001", Skill = QuestionSkill.Reading, IsActive = true };

            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: draftQb);
            var qtRepo  = GetActiveQtRepo(QuestionSkill.Reading);
            var optRepo = GetOptionRepoMock();
            var handler = CreateHandler(qbRepo: qbRepo, optRepo: optRepo, qtRepo: qtRepo);
            var command = new UpdateQuestionBankCommand
            {
                QuestionBankId = draftQb.QuestionBankId,
                QuestionTypeId = "QT-001",
                Content        = "Updated content"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be(draftQb.QuestionBankId);
            qbRepo.Verify(x => x.UpdateAsync(It.IsAny<QuestionBank>()), Times.Once);
            qbRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionBank",
                TestCaseID        = "UpdateQuestionBank_06",
                Description       = "Happy path: Draft QB content updated, UpdateAsync+SaveChanges called, 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200, Data=QB id",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Draft", "Content updated", "200 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // UpdateQuestionBank_07 | A | Repository throws on UpdateAsync → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            // Arrange
            var draftQb = MockQuestionBankRepository.GetSampleDraftQB();
            draftQb.QuestionTypeId = "QT-001";
            draftQb.QuestionType   = new QuestionType { Skill = QuestionSkill.Reading, IsActive = true };

            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(draftQb);
            qbRepo.Setup(x => x.UpdateAsync(It.IsAny<QuestionBank>()))
                  .ThrowsAsync(new InvalidOperationException("DB error"));

            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new UpdateQuestionBankCommand
            {
                QuestionBankId = draftQb.QuestionBankId,
                QuestionTypeId = "QT-001"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Question Bank - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionBank",
                TestCaseID        = "UpdateQuestionBank_07",
                Description       = "UpdateAsync throws exception → caught → 500 ServerError",
                ExpectedResult    = "IsSuccess=false, StatusCode=500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UpdateAsync throws", "catch block returns 500" }
            });
        }
    }
}
