using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks
{
    public class CreateQuestionBankCommandHandlerTests
    {
        // ─────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────
        private static Mock<IQuestionTypeRepository> GetActiveQtRepo(
            string id    = "QT-001",
            QuestionSkill skill = QuestionSkill.Reading)
        {
            var mock = new Mock<IQuestionTypeRepository>();
            mock.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QuestionType
                {
                    QuestionTypeId = id,
                    Skill          = skill,
                    IsActive       = true,
                    Name           = "Sample Type"
                });
            return mock;
        }

        private static Mock<IIdGeneratorService> GetIdGenMock(string id = "QB-CREATE-001")
        {
            var mock = new Mock<IIdGeneratorService>();
            mock.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns(id);
            return mock;
        }

        private static Mock<IQuestionOptionRepository> GetOptionRepoMock()
        {
            var mock = new Mock<IQuestionOptionRepository>();
            mock.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionOption>>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            return mock;
        }

        private static CreateQuestionBankCommandHandler CreateHandler(
            Mock<IQuestionBankRepository>?  qbRepo      = null,
            Mock<IQuestionOptionRepository>? optRepo     = null,
            Mock<IQuestionTypeRepository>?   qtRepo      = null,
            Mock<IPassageRepository>?        passageRepo = null,
            Mock<IIdGeneratorService>?       idGen       = null)
        {
            return new CreateQuestionBankCommandHandler(
                (qbRepo      ?? MockQuestionBankRepository.GetMock()).Object,
                (optRepo     ?? GetOptionRepoMock()).Object,
                (qtRepo      ?? GetActiveQtRepo()).Object,
                (passageRepo ?? new Mock<IPassageRepository>()).Object,
                (idGen       ?? GetIdGenMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRA-01 | A | Empty QuestionTypeId → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyQuestionTypeId_ShouldReturn400()
        {
            // Arrange
            var handler = CreateHandler();
            var command = new CreateQuestionBankCommand { QuestionTypeId = "" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBank",
                TestCaseID        = "TC-QB-CRA-01",
                Description       = "Empty QuestionTypeId → 400 ValidationFailed",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionTypeId = empty string after trim", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRA-02 | A | QuestionType not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_QuestionTypeNotFound_ShouldReturn404()
        {
            // Arrange
            var qtRepo = new Mock<IQuestionTypeRepository>();
            qtRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((QuestionType?)null);
            var handler = CreateHandler(qtRepo: qtRepo);
            var command = new CreateQuestionBankCommand { QuestionTypeId = "QT-MISSING" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBank",
                TestCaseID        = "TC-QB-CRA-02",
                Description       = "QuestionType not found → 404 QuestionTypeNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "404 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRA-03 | A | QuestionType inactive → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_InactiveQuestionType_ShouldReturn400()
        {
            // Arrange
            var qtRepo = new Mock<IQuestionTypeRepository>();
            qtRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new QuestionType { QuestionTypeId = "QT-001", IsActive = false, Skill = QuestionSkill.Reading });
            var handler = CreateHandler(qtRepo: qtRepo);
            var command = new CreateQuestionBankCommand { QuestionTypeId = "QT-001" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBank",
                TestCaseID        = "TC-QB-CRA-03",
                Description       = "Inactive QuestionType → 400 ValidationFailed",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionType.IsActive=false", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRA-04 | A | Listening skill without MediaUrl → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ListeningWithoutMediaUrl_ShouldReturn400()
        {
            // Arrange
            var qtRepo  = GetActiveQtRepo(skill: QuestionSkill.Listening);
            var handler = CreateHandler(qtRepo: qtRepo);
            var command = new CreateQuestionBankCommand
            {
                QuestionTypeId = "QT-001",
                MediaUrl       = null // Missing
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBank",
                TestCaseID        = "TC-QB-CRA-04",
                Description       = "Listening skill without MediaUrl → 400 ValidationFailed",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill=Listening", "MediaUrl=null", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRA-05 | A | Reading skill without Content → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ReadingWithoutContent_ShouldReturn400()
        {
            // Arrange
            var qtRepo  = GetActiveQtRepo(skill: QuestionSkill.Reading);
            var handler = CreateHandler(qtRepo: qtRepo);
            var command = new CreateQuestionBankCommand
            {
                QuestionTypeId = "QT-001",
                Content        = "" // Empty content for Reading
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBank",
                TestCaseID        = "TC-QB-CRA-05",
                Description       = "Reading skill with empty Content → 400 ValidationFailed",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill=Reading", "Content=empty", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRA-06 | N | Happy path: Reading QB created as Draft, 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidReadingQuestion_ShouldCreateDraftAndReturn201()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock();
            var optRepo = GetOptionRepoMock();
            var qtRepo  = GetActiveQtRepo(skill: QuestionSkill.Reading);
            var idGen   = GetIdGenMock("QB-CREATE-001");
            var handler = CreateHandler(qbRepo: qbRepo, optRepo: optRepo, qtRepo: qtRepo, idGen: idGen);
            var command = new CreateQuestionBankCommand
            {
                QuestionTypeId = "QT-001",
                Content        = "What does 'benevolent' mean?",
                CreateBy       = "ADMIN-001",
                Options        = new List<CreateQuestionOptionDto>
                {
                    new CreateQuestionOptionDto { KeyOption = "1", Content = "Kind and generous", IsCorrect = true  },
                    new CreateQuestionOptionDto { KeyOption = "2", Content = "Cruel and selfish",  IsCorrect = false }
                }
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("QB-CREATE-001");
            qbRepo.Verify(x => x.AddAsync(It.Is<QuestionBank>(
                qb => qb.Status == QuestionBankStatus.Draft && qb.CreateBy == "ADMIN-001")), Times.Once);
            optRepo.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionOption>>()), Times.Once);
            qbRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBank",
                TestCaseID        = "TC-QB-CRA-06",
                Description       = "Happy path: Reading QB created as Draft, AddAsync+SaveChanges called, 201 returned",
                ExpectedResult    = "IsSuccess=true, StatusCode=201, Data='QB-CREATE-001', Status=Draft",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill=Reading", "QB.Status=Draft", "AddRangeAsync once", "201 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRA-07 | N | Writing skill → QB created, AddRangeAsync NOT called
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WritingSkill_ShouldNotAddOptions()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock();
            var optRepo = GetOptionRepoMock();
            var qtRepo  = GetActiveQtRepo(skill: QuestionSkill.Writing);
            var handler = CreateHandler(qbRepo: qbRepo, optRepo: optRepo, qtRepo: qtRepo);
            var command = new CreateQuestionBankCommand
            {
                QuestionTypeId = "QT-WRITE-01",
                Content        = "Write an essay about technology.",
                CreateBy       = "ADMIN-001",
                Options        = new List<CreateQuestionOptionDto>()
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            optRepo.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionOption>>()), Times.Never);

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBank",
                TestCaseID        = "TC-QB-CRA-07",
                Description       = "Writing skill QB: no options added (AddRangeAsync not called), 201 returned",
                ExpectedResult    = "IsSuccess=true, StatusCode=201, AddRangeAsync Times.Never",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill=Writing", "options skipped", "201 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRA-08 | N | Passage not found when PassageId provided → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_PassageNotFound_ShouldReturn404()
        {
            // Arrange
            var passageRepo = new Mock<IPassageRepository>();
            passageRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((Passage?)null);
            var handler = CreateHandler(passageRepo: passageRepo);
            var command = new CreateQuestionBankCommand
            {
                QuestionTypeId = "QT-001",
                Content        = "Some question",
                PassageId      = "PASSAGE-MISSING"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBank",
                TestCaseID        = "TC-QB-CRA-08",
                Description       = "PassageId provided but passage not found → 404 PassageNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "PassageId non-empty", "GetByIdAsync returns null", "404 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRA-09 | A | Repository throws on AddAsync → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            // Arrange
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.AddAsync(It.IsAny<QuestionBank>()))
                  .ThrowsAsync(new InvalidOperationException("DB write error"));
            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new CreateQuestionBankCommand
            {
                QuestionTypeId = "QT-001",
                Content        = "Some content",
                CreateBy       = "ADMIN-001"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Question Bank - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBank",
                TestCaseID        = "TC-QB-CRA-09",
                Description       = "Repository AddAsync throws exception → caught in try/catch → 500 ServerError",
                ExpectedResult    = "IsSuccess=false, StatusCode=500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws", "catch block returns 500" }
            });
        }
    }
}
