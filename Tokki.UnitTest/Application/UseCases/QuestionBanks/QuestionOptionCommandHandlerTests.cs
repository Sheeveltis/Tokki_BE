using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Create;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Delete;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Update;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks
{
    public class QuestionOptionCommandHandlerTests
    {
        // ─────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────
        private static Mock<IQuestionTypeRepository> GetQtRepoMock(QuestionSkill skill = QuestionSkill.Reading)
        {
            var mock = new Mock<IQuestionTypeRepository>();
            mock.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QuestionType { QuestionTypeId = "QT-001", Skill = skill, IsActive = true });
            return mock;
        }

        private static Mock<IIdGeneratorService> GetIdGenMock(string id = "OPT-NEW-001")
        {
            var mock = new Mock<IIdGeneratorService>();
            mock.Setup(m => m.GenerateCustom(It.IsAny<int>())).Returns(id);
            return mock;
        }

        private static Mock<IQuestionOptionRepository> GetOptionRepoMock()
        {
            var mock = new Mock<IQuestionOptionRepository>();
            mock.Setup(x => x.AddAsync(It.IsAny<QuestionOption>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.UpdateAsync(It.IsAny<QuestionOption>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.DeleteAsync(It.IsAny<QuestionOption>())).Returns(Task.CompletedTask);
            mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            return mock;
        }

        private static QuestionBank MakeDraftQBWithOptions(List<QuestionOption>? options = null)
        {
            return new QuestionBank
            {
                QuestionBankId  = "QB-OPT-01",
                Status          = QuestionBankStatus.Draft,
                QuestionTypeId  = "QT-001",
                QuestionType    = new QuestionType { QuestionTypeId = "QT-001", Skill = QuestionSkill.Reading, IsActive = true },
                QuestionOptions = options ?? new List<QuestionOption>()
            };
        }

        // ═════════════════════════════════════════════════════════
        //  CREATE QUESTION OPTION
        // ═════════════════════════════════════════════════════════

        // CreateQuestionOption_01 | A | QB not found → 404
        [Fact]
        public async Task CreateOption_QBNotFound_ShouldReturn404()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: null);
            var handler = new CreateQuestionOptionCommandHandler(
                qbRepo.Object,
                GetOptionRepoMock().Object,
                GetQtRepoMock().Object,
                GetIdGenMock().Object);
            var command = new CreateQuestionOptionCommand { QuestionBankId = "QB-MISSING", KeyOption = "1", Content = "A" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionOption",
                TestCaseID        = "CreateQuestionOption_01",
                Description       = "QB not found → 404 QuestionBankNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdWithDetailsAsync returns null", "404" }
            });
        }

        // CreateQuestionOption_02 | A | QB not Draft → 403
        [Fact]
        public async Task CreateOption_QBNotDraft_ShouldReturn403()
        {
            // Arrange
            var activeQb = MockQuestionBankRepository.GetSampleActiveQB();
            activeQb.QuestionTypeId = "QT-001";
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: activeQb);
            var handler = new CreateQuestionOptionCommandHandler(
                qbRepo.Object,
                GetOptionRepoMock().Object,
                GetQtRepoMock().Object,
                GetIdGenMock().Object);
            var command = new CreateQuestionOptionCommand { QuestionBankId = activeQb.QuestionBankId, KeyOption = "1", Content = "A" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionOption",
                TestCaseID        = "CreateQuestionOption_02",
                Description       = "QB not in Draft status → 403 Forbidden",
                ExpectedResult    = "IsSuccess=false, StatusCode=403",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Active", "only Draft allowed", "403" }
            });
        }

        // CreateQuestionOption_03 | A | Writing skill → 400
        [Fact]
        public async Task CreateOption_WritingSkill_ShouldReturn400()
        {
            // Arrange
            var writingQb = MakeDraftQBWithOptions();
            writingQb.QuestionType = new QuestionType { Skill = QuestionSkill.Writing, IsActive = true };
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: writingQb);
            var qtRepo  = GetQtRepoMock(QuestionSkill.Writing);
            var handler = new CreateQuestionOptionCommandHandler(
                qbRepo.Object, GetOptionRepoMock().Object, qtRepo.Object, GetIdGenMock().Object);
            var command = new CreateQuestionOptionCommand { QuestionBankId = "QB-OPT-01", KeyOption = "1", Content = "Something" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionOption",
                TestCaseID        = "CreateQuestionOption_03",
                Description       = "Writing skill QB: cannot add options → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill=Writing", "400 returned" }
            });
        }

        // CreateQuestionOption_04 | A | Already 4 options → 400
        [Fact]
        public async Task CreateOption_MaxOptions_ShouldReturn400()
        {
            // Arrange — QB already has 4 options
            var opts = new List<QuestionOption>
            {
                new QuestionOption { OptionId = "O1", KeyOption = "1" },
                new QuestionOption { OptionId = "O2", KeyOption = "2" },
                new QuestionOption { OptionId = "O3", KeyOption = "3" },
                new QuestionOption { OptionId = "O4", KeyOption = "4" }
            };
            var qb     = MakeDraftQBWithOptions(opts);
            var qbRepo = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: qb);
            var handler = new CreateQuestionOptionCommandHandler(
                qbRepo.Object, GetOptionRepoMock().Object, GetQtRepoMock().Object, GetIdGenMock().Object);
            var command = new CreateQuestionOptionCommand { QuestionBankId = "QB-OPT-01", KeyOption = "5", Content = "Extra" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionOption",
                TestCaseID        = "CreateQuestionOption_04",
                Description       = "Already 4 options → cannot add more → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "currentOptions.Count >= 4", "400 returned" }
            });
        }

        // CreateQuestionOption_05 | N | Happy path: Reading, < 4 options → option created, 201
        [Fact]
        public async Task CreateOption_ValidReadingQB_ShouldReturn201()
        {
            // Arrange
            var qb = MakeDraftQBWithOptions(new List<QuestionOption>
            {
                new QuestionOption { OptionId = "O1", KeyOption = "1", Content = "A", IsCorrect = true }
            });
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: qb);
            var optRepo = GetOptionRepoMock();
            var handler = new CreateQuestionOptionCommandHandler(
                qbRepo.Object, optRepo.Object, GetQtRepoMock().Object, GetIdGenMock().Object);
            var command = new CreateQuestionOptionCommand
            {
                QuestionBankId = "QB-OPT-01",
                KeyOption      = "2",
                Content        = "Option B",
                IsCorrect      = false
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            optRepo.Verify(x => x.AddAsync(It.IsAny<QuestionOption>()), Times.Once);
            optRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionOption",
                TestCaseID        = "CreateQuestionOption_05",
                Description       = "Happy path: valid option added to Draft Reading QB, AddAsync+SaveChanges called, 201",
                ExpectedResult    = "IsSuccess=true, StatusCode=201",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill=Reading", "< 4 options", "no dup key", "201 returned" }
            });
        }

        // CreateQuestionOption_06 | A | Duplicate KeyOption → 400
        [Fact]
        public async Task CreateOption_DuplicateKey_ShouldReturn400()
        {
            // Arrange
            var existingOpts = new List<QuestionOption>
            {
                new QuestionOption { OptionId = "O1", KeyOption = "1", Content = "A" }
            };
            var qb      = MakeDraftQBWithOptions(existingOpts);
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: qb);
            var handler = new CreateQuestionOptionCommandHandler(
                qbRepo.Object, GetOptionRepoMock().Object, GetQtRepoMock().Object, GetIdGenMock().Object);
            var command = new CreateQuestionOptionCommand
            {
                QuestionBankId = "QB-OPT-01",
                KeyOption      = "1", // Duplicate!
                Content        = "Dupliate"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup     = "CreateQuestionOption",
                TestCaseID        = "CreateQuestionOption_06",
                Description       = "Duplicate KeyOption in QB → 400",
                ExpectedResult    = "IsSuccess=false, StatusCode=400",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "KeyOption='1' already exists", "400 returned" }
            });
        }

        // ═════════════════════════════════════════════════════════
        //  DELETE QUESTION OPTION
        // ═════════════════════════════════════════════════════════

        // DeleteQuestionOption_01 | A | QB not found → 404
        [Fact]
        public async Task DeleteOption_QBNotFound_ShouldReturn404()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: null);
            var handler = new DeleteQuestionOptionCommandHandler(qbRepo.Object, GetOptionRepoMock().Object);
            var command = new DeleteQuestionOptionCommand { QuestionBankId = "QB-MISSING", OptionId = "OPT-1" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank Option - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionOption",
                TestCaseID        = "DeleteQuestionOption_01",
                Description       = "QB not found → 404 QuestionBankNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdWithDetailsAsync null", "404" }
            });
        }

        // DeleteQuestionOption_02 | A | QB not Draft → 403
        [Fact]
        public async Task DeleteOption_QBNotDraft_ShouldReturn403()
        {
            // Arrange
            var activeQb = MockQuestionBankRepository.GetSampleActiveQB();
            activeQb.QuestionTypeId = "QT-001";
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: activeQb);
            var handler = new DeleteQuestionOptionCommandHandler(qbRepo.Object, GetOptionRepoMock().Object);
            var command = new DeleteQuestionOptionCommand { QuestionBankId = activeQb.QuestionBankId, OptionId = "OPT-1" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);

            QACollector.LogTestCase("Question Bank Option - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionOption",
                TestCaseID        = "DeleteQuestionOption_02",
                Description       = "QB not in Draft → 403 (only Draft can delete options)",
                ExpectedResult    = "IsSuccess=false, StatusCode=403",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Active", "403 returned" }
            });
        }

        // DeleteQuestionOption_03 | A | Option not found in QB → 404
        [Fact]
        public async Task DeleteOption_OptionNotFound_ShouldReturn404()
        {
            // Arrange
            var qb     = MakeDraftQBWithOptions(); // no options
            var qbRepo = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: qb);
            var handler = new DeleteQuestionOptionCommandHandler(qbRepo.Object, GetOptionRepoMock().Object);
            var command = new DeleteQuestionOptionCommand { QuestionBankId = "QB-OPT-01", OptionId = "OPT-MISSING" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank Option - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionOption",
                TestCaseID        = "DeleteQuestionOption_03",
                Description       = "OptionId not in QB.QuestionOptions → 404 QuestionOptionNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "OptionId not matching", "404 returned" }
            });
        }

        // DeleteQuestionOption_04 | N | Happy path: valid option deleted, 200
        [Fact]
        public async Task DeleteOption_ValidOption_ShouldReturn200()
        {
            // Arrange
            var opt = new QuestionOption { OptionId = "OPT-1", KeyOption = "1", Content = "A" };
            var qb  = MakeDraftQBWithOptions(new List<QuestionOption> { opt });
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: qb);
            var optRepo = GetOptionRepoMock();
            var handler = new DeleteQuestionOptionCommandHandler(qbRepo.Object, optRepo.Object);
            var command = new DeleteQuestionOptionCommand { QuestionBankId = "QB-OPT-01", OptionId = "OPT-1" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            optRepo.Verify(x => x.DeleteAsync(It.IsAny<QuestionOption>()), Times.Once);
            optRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank Option - Delete", new TestCaseDetail
            {
                FunctionGroup     = "DeleteQuestionOption",
                TestCaseID        = "DeleteQuestionOption_04",
                Description       = "Happy path: option found and deleted, DeleteAsync+SaveChanges called, 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Draft", "OptionId matches", "200 returned" }
            });
        }

        // ═════════════════════════════════════════════════════════
        //  UPDATE QUESTION OPTION
        // ═════════════════════════════════════════════════════════

        // UpdateQuestionOption_01 | A | QB not found → 404
        [Fact]
        public async Task UpdateOption_QBNotFound_ShouldReturn404()
        {
            // Arrange
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: null);
            var handler = new UpdateQuestionOptionCommandHandler(
                qbRepo.Object, GetOptionRepoMock().Object, GetQtRepoMock().Object);
            var command = new UpdateQuestionOptionCommand { QuestionBankId = "QB-MISSING", OptionId = "OPT-1" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank Option - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionOption",
                TestCaseID        = "UpdateQuestionOption_01",
                Description       = "QB not found → 404",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdWithDetailsAsync null", "404" }
            });
        }

        // UpdateQuestionOption_02 | A | OptionId not found → 404
        [Fact]
        public async Task UpdateOption_OptionNotFound_ShouldReturn404()
        {
            // Arrange
            var qb     = MakeDraftQBWithOptions(); // empty options
            var qbRepo = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: qb);
            var handler = new UpdateQuestionOptionCommandHandler(
                qbRepo.Object, GetOptionRepoMock().Object, GetQtRepoMock().Object);
            var command = new UpdateQuestionOptionCommand { QuestionBankId = "QB-OPT-01", OptionId = "OPT-MISSING", Content = "X" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Question Bank Option - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionOption",
                TestCaseID        = "UpdateQuestionOption_02",
                Description       = "OptionId not found in QB → 404 QuestionOptionNotFound",
                ExpectedResult    = "IsSuccess=false, StatusCode=404",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "OptionId not in QB.QuestionOptions", "404" }
            });
        }

        // UpdateQuestionOption_03 | N | Happy path: update option content, 200
        [Fact]
        public async Task UpdateOption_ValidUpdate_ShouldReturn200()
        {
            // Arrange
            var opt = new QuestionOption { OptionId = "OPT-1", KeyOption = "1", Content = "Old content", IsCorrect = true };
            var qb  = MakeDraftQBWithOptions(new List<QuestionOption> { opt });
            var qbRepo  = MockQuestionBankRepository.GetMock(returnedByIdWithDetails: qb);
            var optRepo = GetOptionRepoMock();
            var handler = new UpdateQuestionOptionCommandHandler(
                qbRepo.Object, optRepo.Object, GetQtRepoMock().Object);
            var command = new UpdateQuestionOptionCommand
            {
                QuestionBankId = "QB-OPT-01",
                OptionId       = "OPT-1",
                Content        = "Updated content"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            optRepo.Verify(x => x.UpdateAsync(It.IsAny<QuestionOption>()), Times.Once);
            optRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank Option - Update", new TestCaseDetail
            {
                FunctionGroup     = "UpdateQuestionOption",
                TestCaseID        = "UpdateQuestionOption_03",
                Description       = "Happy path: content updated, UpdateAsync+SaveChanges called, 200",
                ExpectedResult    = "IsSuccess=true, StatusCode=200",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QB.Status=Draft", "OptionId matches", "Content updated", "200" }
            });
        }
    }
}
