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
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Update;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands.QuestionOptions
{
    public class UpdateQuestionOptionCommandHandlerTests
    {
        private readonly Mock<IQuestionBankRepository> _questionBankRepoMock = new();
        private readonly Mock<IQuestionOptionRepository> _questionOptionRepoMock = new();
        private readonly Mock<IQuestionTypeRepository> _questionTypeRepoMock = new();

        public UpdateQuestionOptionCommandHandlerTests()
        {
        }

        private UpdateQuestionOptionCommandHandler CreateHandler()
        {
            return new UpdateQuestionOptionCommandHandler(
                _questionBankRepoMock.Object,
                _questionOptionRepoMock.Object,
                _questionTypeRepoMock.Object);
        }

        private QuestionBank SetupValidQuestionBankDraft(string skill = "Reading")
        {
            var qt = new QuestionType { Skill = Enum.Parse<QuestionSkill>(skill) };
            var qb = new QuestionBank 
            { 
                QuestionBankId = "qb-1",
                Status = QuestionBankStatus.Draft, 
                QuestionTypeId = "qt-1",
                QuestionOptions = new List<QuestionOption> 
                { 
                    new QuestionOption { OptionId = "opt-1", KeyOption = "A", IsCorrect = true, Content = "Content A" },
                    new QuestionOption { OptionId = "opt-2", KeyOption = "B", IsCorrect = false, Content = "Content B" }
                }
            };
            
            _questionBankRepoMock.Setup(x => x.GetByIdWithDetailsAsync("qb-1", It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(qb);
            _questionTypeRepoMock.Setup(x => x.GetByIdAsync("qt-1", It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(qt);
            return qb;
        }

        // -----------------------------------------------------------
        // UpdateQuestionOptionCommandHandler_01 | A | Missing OptionId -> 404
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_OptionNotFound_ShouldReturn404()
        {
            SetupValidQuestionBankDraft();
            var command = new UpdateQuestionOptionCommand { QuestionBankId = "qb-1", OptionId = "invalid-opt" };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Errors.Should().ContainSingle(e => e.Code == AppErrors.QuestionOptionNotFound.Code);

            QACollector.LogTestCase("Question Bank Option - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionOptionCommandHandler",
                TestCaseID = "UpdateQuestionOptionCommandHandler_01",
                Description = "Returns error if Option is not found",
                ExpectedResult = "Return 404 QuestionOptionNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Option not in qb.QuestionOptions" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionOptionCommandHandler_02 | A | Duplicate New KeyOption -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_DuplicateKeyOption_ShouldReturn400()
        {
            SetupValidQuestionBankDraft();
            var command = new UpdateQuestionOptionCommand { QuestionBankId = "qb-1", OptionId = "opt-1", KeyOption = "B" };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.First().Description.Should().Contain("dã t?n t?i");

            QACollector.LogTestCase("Question Bank Option - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionOptionCommandHandler",
                TestCaseID = "UpdateQuestionOptionCommandHandler_02",
                Description = "Rejects update if new KeyOption conflicts with other options",
                ExpectedResult = "Return 400",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "KeyOption duplicates sibling" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionOptionCommandHandler_03 | A | Clear Content/Image Fails -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ContentImageCleared_ShouldReturn400()
        {
            var qb = SetupValidQuestionBankDraft();
            // Erase content directly in DB so that incoming command"" triggers empty validation
            qb.QuestionOptions.First(o => o.OptionId == "opt-1").Content = "";
            
            var command = new UpdateQuestionOptionCommand { QuestionBankId = "qb-1", OptionId = "opt-1", KeyOption = "C", Content = "" };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("Question Bank Option - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionOptionCommandHandler",
                TestCaseID = "UpdateQuestionOptionCommandHandler_03",
                Description = "Blocks update resulting in empty content and image",
                ExpectedResult = "Return 400",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Content/Image null after update" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionOptionCommandHandler_04 | A | Remove Single Correct Option -> 400
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_RemoveSingleCorrect_ShouldReturn400()
        {
            SetupValidQuestionBankDraft(); // opt-1 is the ONLY correct one
            var command = new UpdateQuestionOptionCommand { QuestionBankId = "qb-1", OptionId = "opt-1", IsCorrect = false };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.First().Description.Should().Contain("ít nh?t m?t dáp án dúng");

            QACollector.LogTestCase("Question Bank Option - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionOptionCommandHandler",
                TestCaseID = "UpdateQuestionOptionCommandHandler_04",
                Description = "Blocks un-checking IsCorrect if it's the only correct option",
                ExpectedResult = "Return 400 ValidationFailed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsCorrect=false on only correct option" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionOptionCommandHandler_05 | N | Successfully Updating Toggles Other Correct
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_SuccessUpdate_ToggleCorrect_ShouldReturn200()
        {
            SetupValidQuestionBankDraft(); // opt-1 is correct
            // Now we make opt-2 correct
            var command = new UpdateQuestionOptionCommand { QuestionBankId = "qb-1", OptionId = "opt-2", IsCorrect = true };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            // Verify opt-1 becomes false
            _questionOptionRepoMock.Verify(x => x.UpdateAsync(It.Is<QuestionOption>(o => o.OptionId == "opt-1" && o.IsCorrect == false)), Times.Once);
            
            // Verify opt-2 becomes true and saves
            _questionOptionRepoMock.Verify(x => x.UpdateAsync(It.Is<QuestionOption>(o => o.OptionId == "opt-2" && o.IsCorrect == true)), Times.Once);
            _questionOptionRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Question Bank Option - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionOptionCommandHandler",
                TestCaseID = "UpdateQuestionOptionCommandHandler_05",
                Description = "Sets new option correctly and un-sets existing correct ones",
                ExpectedResult = "Return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IsCorrect=true on another option" }
            });
        }

        // -----------------------------------------------------------
        // UpdateQuestionOptionCommandHandler_06 | N | Basic Modification Success
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_SuccessUpdate_BasicFields_ShouldReturn200()
        {
            SetupValidQuestionBankDraft();
            var command = new UpdateQuestionOptionCommand { QuestionBankId = "qb-1", OptionId = "opt-2", Content = "Updated B", KeyOption = "C" };
            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);

            _questionOptionRepoMock.Verify(x => x.UpdateAsync(It.Is<QuestionOption>(o => o.OptionId == "opt-2" && o.Content == "Updated B" && o.KeyOption == "C")), Times.Once);

            QACollector.LogTestCase("Question Bank Option - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdateQuestionOptionCommandHandler",
                TestCaseID = "UpdateQuestionOptionCommandHandler_06",
                Description = "Correctly modifies basic fields without toggling correct status",
                ExpectedResult = "Return 200",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Content, KeyOption update" }
            });
        }
    }
}
