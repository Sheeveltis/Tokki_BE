using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Create;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands.QuestionOptions
{
    public class CreateQuestionOptionCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _questionBankRepoMock = new();
        private readonly Mock<IQuestionOptionRepository> _questionOptionRepoMock = new();
        private readonly Mock<IQuestionTypeRepository> _questionTypeRepoMock = new();
        private readonly Mock<IIdGeneratorService> _idGenMock = new();

        public CreateQuestionOptionCommandHandlerTests()
        {
            _idGenMock.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("opt-id");
            _idGenMock.Setup(x => x.Generate(It.IsAny<int>())).Returns("gen-id");
        }

        private CreateQuestionOptionCommandHandler CreateHandler()
        {
            return new CreateQuestionOptionCommandHandler(
                _questionBankRepoMock.Object,
                _questionOptionRepoMock.Object,
                _questionTypeRepoMock.Object,
                _idGenMock.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // CreateQuestionOptionCommandHandler_01 | A | Question Bank NotFound -> 404
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_QuestionBankNotFound_ShouldReturn404()
        {
            var command = new CreateQuestionOptionCommand { QuestionBankId = "qb-1", KeyOption = "A" };
            _questionBankRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((QuestionBank?)null);

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.QuestionBankNotFound.Code);

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandHandler",
                TestCaseID = "CreateQuestionOptionCommandHandler_01",
                Description = "Returns error if QuestionBank is not found",
                ExpectedResult = "Return 404 QuestionBankNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetByIdWithDetailsAsync returns null" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateQuestionOptionCommandHandler_02 | A | Not Draft Status -> 403
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_NotDraft_ShouldReturn403()
        {
            var command = new CreateQuestionOptionCommand { QuestionBankId = "qb-1", KeyOption = "A" };
            var qb = new QuestionBank { Status = QuestionBankStatus.Active };
            _questionBankRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(qb);

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(403);
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.Forbidden.Code);

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandHandler",
                TestCaseID = "CreateQuestionOptionCommandHandler_02",
                Description = "Cannot edit options on non-draft question bank",
                ExpectedResult = "Return 403 Forbidden",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionBank Status is not Draft" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateQuestionOptionCommandHandler_03 | A | Missing QuestionTypeId -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_MissingQuestionTypeId_ShouldReturn400()
        {
            var command = new CreateQuestionOptionCommand { QuestionBankId = "qb-1", KeyOption = "A" };
            var qb = new QuestionBank { Status = QuestionBankStatus.Draft, QuestionTypeId = "  " };
            _questionBankRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(qb);

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandHandler",
                TestCaseID = "CreateQuestionOptionCommandHandler_03",
                Description = "Returns error if QuestionBank lacks a QuestionTypeId",
                ExpectedResult = "Return 400 ValidationFailed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionTypeId is empty/whitespace" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateQuestionOptionCommandHandler_04 | A | Writing Skill Question -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_WritingSkill_ShouldReturn400()
        {
            var command = new CreateQuestionOptionCommand { QuestionBankId = "qb-1", KeyOption = "A" };
            var qb = new QuestionBank { Status = QuestionBankStatus.Draft, QuestionTypeId = "qt-1" };
            var qt = new QuestionType { Skill = QuestionSkill.Writing };

            _questionBankRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            _questionTypeRepoMock.Setup(x => x.GetByIdAsync("qt-1", It.IsAny<CancellationToken>())).ReturnsAsync(qt);

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandHandler",
                TestCaseID = "CreateQuestionOptionCommandHandler_04",
                Description = "Cannot add MCQ options to Writing questions",
                ExpectedResult = "Return 400 ValidationFailed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "QuestionType Skill is Writing" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateQuestionOptionCommandHandler_05 | B | More than 4 Options limit -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_OptionLimitExceeded_ShouldReturn400()
        {
            var command = new CreateQuestionOptionCommand { QuestionBankId = "qb-1", KeyOption = "E" };
            var qb = new QuestionBank 
            { 
                Status = QuestionBankStatus.Draft, 
                QuestionTypeId = "qt-1",
                QuestionOptions = new List<QuestionOption> 
                {
                    new QuestionOption { KeyOption = "A" },
                    new QuestionOption { KeyOption = "B" },
                    new QuestionOption { KeyOption = "C" },
                    new QuestionOption { KeyOption = "D" }
                }
            };
            var qt = new QuestionType { Skill = QuestionSkill.Reading };

            _questionBankRepoMock.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            _questionTypeRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(qt);

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.First().Description.Should().Contain("quá 4 đáp án");

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandHandler",
                TestCaseID = "CreateQuestionOptionCommandHandler_05",
                Description = "Rejects creation when there are already 4 options",
                ExpectedResult = "Return 400 ValidationFailed",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "options.Count >= 4" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateQuestionOptionCommandHandler_06 | A | Duplicate Key Option -> 400
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_DuplicateKeyOption_ShouldReturn400()
        {
            var command = new CreateQuestionOptionCommand { QuestionBankId = "qb-1", KeyOption = "A" };
            var qb = new QuestionBank 
            { 
                Status = QuestionBankStatus.Draft, 
                QuestionTypeId = "qt-1",
                QuestionOptions = new List<QuestionOption> { new QuestionOption { KeyOption = "A" } }
            };
            var qt = new QuestionType { Skill = QuestionSkill.Reading };

            _questionBankRepoMock.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            _questionTypeRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(qt);

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.First().Description.Should().Contain("đã tồn tại");

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandHandler",
                TestCaseID = "CreateQuestionOptionCommandHandler_06",
                Description = "Rejects creation when KeyOption exists",
                ExpectedResult = "Return 400 ValidationFailed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Duplicate KeyOption" }
            });
        }

        // ═══════════════════════════════════════════════════════════
        // CreateQuestionOptionCommandHandler_07 | N | Successful Creation Toggling Correct Option -> 201
        // ═══════════════════════════════════════════════════════════
        [Fact]
        public async Task Handle_SuccessfulCreation_TogglesCorrectOption_ShouldReturn201()
        {
            var command = new CreateQuestionOptionCommand { QuestionBankId = "qb-1", KeyOption = "B", Content = "Test", IsCorrect = true };
            var oldCorrect = new QuestionOption { OptionId = "opt-A", KeyOption = "A", IsCorrect = true };
            var qb = new QuestionBank 
            { 
                QuestionBankId = "qb-1",
                Status = QuestionBankStatus.Draft, 
                QuestionTypeId = "qt-1",
                QuestionOptions = new List<QuestionOption> { oldCorrect }
            };
            var qt = new QuestionType { Skill = QuestionSkill.Reading };

            _questionBankRepoMock.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(qb);
            _questionTypeRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(qt);

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            
            // Verify old option was toggled to false
            _questionOptionRepoMock.Verify(x => x.UpdateAsync(It.Is<QuestionOption>(o => o.OptionId == "opt-A" && o.IsCorrect == false)), Times.Once);
            
            // Verify new option was added
            _questionOptionRepoMock.Verify(x => x.AddAsync(It.Is<QuestionOption>(o => o.KeyOption == "B" && o.IsCorrect == true)), Times.Once);
            _questionOptionRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank Option - Create", new TestCaseDetail
            {
                FunctionGroup = "CreateQuestionOptionCommandHandler",
                TestCaseID = "CreateQuestionOptionCommandHandler_07",
                Description = "Creates option successfully and toggles other correct options to false",
                ExpectedResult = "Return 201",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Normal creation", "IsCorrect = true" }
            });
        }
    }
}
