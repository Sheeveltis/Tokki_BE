using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBankByStaff;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks
{
    public class CreateQuestionBankByStaffCommandHandlerTests
    {
        private static Mock<IQuestionTypeRepository> GetActiveQuestionTypeRepo(
            string questionTypeId = "QT-001",
            QuestionSkill skill   = QuestionSkill.Reading)
        {
            var mockRepo = new Mock<IQuestionTypeRepository>();
            mockRepo.Setup(x => x.GetByIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QuestionType
                    {
                        QuestionTypeId = questionTypeId,
                        Skill          = skill,
                        IsActive       = true,
                        Name           = "Sample Type"
                    });
            return mockRepo;
        }

        private static Mock<IIdGeneratorService> GetIdGeneratorMock(string id = "QB-GEN-001")
        {
            var mock = new Mock<IIdGeneratorService>();
            mock.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns(id);
            return mock;
        }

        private static CreateQuestionBankByStaffCommandHandler CreateHandler(
            Mock<IQuestionBankRepository>?  qbRepo      = null,
            Mock<IQuestionOptionRepository>? optRepo     = null,
            Mock<IQuestionTypeRepository>?   qtRepo      = null,
            Mock<IPassageRepository>?        passageRepo = null,
            Mock<IIdGeneratorService>?       idGen       = null)
        {
            var optionRepo = optRepo ?? new Mock<IQuestionOptionRepository>();
            optionRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionOption>>()))
                      .Returns(Task.CompletedTask);
            optionRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

            return new CreateQuestionBankByStaffCommandHandler(
                (qbRepo      ?? MockQuestionBankRepository.GetMock()).Object,
                optionRepo.Object,
                (qtRepo      ?? GetActiveQuestionTypeRepo()).Object,
                (passageRepo ?? new Mock<IPassageRepository>()).Object,
                (idGen       ?? GetIdGeneratorMock()).Object);
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRS-01 | A | Empty QuestionTypeId → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_EmptyQuestionTypeId_ShouldReturn400()
        {
            // Arrange
            var handler = CreateHandler();
            var command = new CreateQuestionBankByStaffCommand { QuestionTypeId = "" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBankByStaff",
                TestCaseID        = "TC-QB-CRS-01",
                Description       = "Empty QuestionTypeId → 400 ValidationFailed",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionTypeId = empty string", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRS-02 | A | QuestionType not found → 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_QuestionTypeNotFound_ShouldReturn404()
        {
            // Arrange
            var qtRepo = new Mock<IQuestionTypeRepository>();
            qtRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((QuestionType?)null);
            var handler = CreateHandler(qtRepo: qtRepo);
            var command = new CreateQuestionBankByStaffCommand { QuestionTypeId = "QT-MISSING" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBankByStaff",
                TestCaseID        = "TC-QB-CRS-02",
                Description       = "QuestionType not found → 404 QuestionTypeNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdAsync returns null", "404 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRS-03 | A | QuestionType inactive → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_InactiveQuestionType_ShouldReturn400()
        {
            // Arrange
            var qtRepo = new Mock<IQuestionTypeRepository>();
            qtRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new QuestionType { QuestionTypeId = "QT-001", IsActive = false, Skill = QuestionSkill.Reading });
            var handler = CreateHandler(qtRepo: qtRepo);
            var command = new CreateQuestionBankByStaffCommand { QuestionTypeId = "QT-001" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBankByStaff",
                TestCaseID        = "TC-QB-CRS-03",
                Description       = "Inactive QuestionType → 400 ValidationFailed",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionType.IsActive=false", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRS-04 | A | Listening skill but no MediaUrl → 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ListeningWithoutMediaUrl_ShouldReturn400()
        {
            // Arrange — Listening QuestionType
            var qtRepo  = GetActiveQuestionTypeRepo(skill: QuestionSkill.Listening);
            var handler = CreateHandler(qtRepo: qtRepo);
            var command = new CreateQuestionBankByStaffCommand
            {
                QuestionTypeId = "QT-001",
                Content        = "Listen carefully",
                MediaUrl       = null // Missing!
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBankByStaff",
                TestCaseID        = "TC-QB-CRS-04",
                Description       = "Listening skill without MediaUrl → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill=Listening", "MediaUrl=null", "400 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRS-05 | N | Happy path: Reading skill → QB created as Draft, 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_ValidReadingQuestion_ShouldCreateDraftAndReturn201()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock();
            var optRepo = new Mock<IQuestionOptionRepository>();
            optRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionOption>>())).Returns(Task.CompletedTask);
            optRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var qtRepo  = GetActiveQuestionTypeRepo(skill: QuestionSkill.Reading);
            var idGen   = GetIdGeneratorMock("QB-NEW-001");
            var handler = CreateHandler(qbRepo: qbRepo, optRepo: optRepo, qtRepo: qtRepo, idGen: idGen);
            var command = new CreateQuestionBankByStaffCommand
            {
                QuestionTypeId = "QT-001",
                Content        = "What is the meaning of 'amiable'?",
                CreateBy       = "STAFF-001",
                Options        = new List<CreateQuestionOptionDto>
                {
                    new CreateQuestionOptionDto { KeyOption = "1", Content = "Friendly", IsCorrect = true  },
                    new CreateQuestionOptionDto { KeyOption = "2", Content = "Hostile",  IsCorrect = false }
                }
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("QB-NEW-001");
            qbRepo.Verify(x => x.AddAsync(It.IsAny<QuestionBank>()), Times.Once);
            qbRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBankByStaff",
                TestCaseID        = "TC-QB-CRS-05",
                Description       = "Happy path: Reading QB created as Draft, AddAsync+SaveChanges called, 201 returned",
                ExpectedResult    = "IsSuccess=true, StatusCode=201, Data='QB-NEW-001'",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill=Reading", "QB created as Draft", "201 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRS-06 | N | Happy path: Writing skill → QB created, NO options added
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WritingSkill_ShouldNotAddOptions()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock();
            var optRepo = new Mock<IQuestionOptionRepository>();
            optRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionOption>>())).Returns(Task.CompletedTask);
            optRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var qtRepo  = GetActiveQuestionTypeRepo(skill: QuestionSkill.Writing);
            var handler = CreateHandler(qbRepo: qbRepo, optRepo: optRepo, qtRepo: qtRepo);
            var command = new CreateQuestionBankByStaffCommand
            {
                QuestionTypeId = "QT-WRITE-01",
                Content        = "Write a paragraph about the environment.",
                CreateBy       = "STAFF-001",
                Options        = new List<CreateQuestionOptionDto>()
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            optRepo.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<QuestionOption>>()), Times.Never);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBankByStaff",
                TestCaseID        = "TC-QB-CRS-06",
                Description       = "Writing skill QB: AddRangeAsync never called (no options), 201 returned",
                ExpectedResult    = "IsSuccess=true, StatusCode=201, AddRangeAsync Times.Never",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill=Writing", "No options created", "201 returned" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // TC-QB-CRS-07 | A | Repository throws → 500
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturn500()
        {
            // Arrange
            var qbRepo = new Mock<IQuestionBankRepository>();
            qbRepo.Setup(x => x.AddAsync(It.IsAny<QuestionBank>()))
                  .ThrowsAsync(new InvalidOperationException("DB error"));

            var handler = CreateHandler(qbRepo: qbRepo);
            var command = new CreateQuestionBankByStaffCommand
            {
                QuestionTypeId = "QT-001",
                Content        = "Some content",
                CreateBy       = "STAFF-001"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);

            QACollector.LogTestCase("Question Bank - Create By Staff", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionBankByStaff",
                TestCaseID        = "TC-QB-CRS-07",
                Description       = "Repository AddAsync throws → caught → 500 ServerError",
                ExpectedResult    = "IsSuccess=false, StatusCode=500",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AddAsync throws exception", "catch block returns 500" }
            });
        }
    }
}
