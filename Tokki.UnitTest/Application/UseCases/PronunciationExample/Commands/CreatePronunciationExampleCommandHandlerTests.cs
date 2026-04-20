using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.PronunciationExample.Commands.CreatePronunciationExample;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationExample.Commands
{
    public class CreatePronunciationExampleCommandHandlerTests
    {
        private readonly Mock<IPronunciationExampleRepository> _mockRepo;
        private readonly Mock<IPronunciationRuleRepository> _mockRuleRepo;
        private readonly Mock<IIdGeneratorService> _mockIdGen;
        private readonly CreatePronunciationExampleCommandHandler _handler;

        public CreatePronunciationExampleCommandHandlerTests()
        {
            _mockRepo = new Mock<IPronunciationExampleRepository>();
            _mockRuleRepo = new Mock<IPronunciationRuleRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();

            _handler = new CreatePronunciationExampleCommandHandler(_mockRepo.Object, _mockRuleRepo.Object, _mockIdGen.Object);
        }

        // CreatePronunciationExampleCommandHandler_01 | A | Rule Not Found -> 404
        [Fact]
        public async Task Handle_RuleNotFound_ShouldReturn404()
        {
            _mockRuleRepo.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync((Domain.Entities.PronunciationRule?)null);

            var command = new CreatePronunciationExampleCommand { PronunciationRuleId = "R1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Be("Rule.NotFound");

            QACollector.LogTestCase("Pronunciation Example - Create", new TestCaseDetail
            {
                FunctionGroup = "CreatePronunciationExampleCommandHandler",
                TestCaseID = "CreatePronunciationExampleCommandHandler_01",
                Description = "Validates parent requirement checking foreign rule key",
                ExpectedResult = "404 Error",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Rule not in DB" }
            });
        }

        // CreatePronunciationExampleCommandHandler_02 | N | Entity Assigned Correct IDs
        [Fact]
        public async Task Handle_ValidInput_AssignsCorrectIds()
        {
            _mockRuleRepo.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync(new Domain.Entities.PronunciationRule());
            _mockIdGen.Setup(x => x.Generate(10)).Returns("12345");

            var command = new CreatePronunciationExampleCommand { PronunciationRuleId = "R1", UserId = "U1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("12345");

            QACollector.LogTestCase("Pronunciation Example - Create", new TestCaseDetail
            {
                FunctionGroup = "CreatePronunciationExampleCommandHandler",
                TestCaseID = "CreatePronunciationExampleCommandHandler_02",
                Description = "Custom generated identities bridge mapping into output signals clearly",
                ExpectedResult = "Success return ID",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ID generation matches return" }
            });
        }

        // CreatePronunciationExampleCommandHandler_03 | N | Tracks Audit Log Safely
        [Fact]
        public async Task Handle_TrackersWork_Succeeds()
        {
            _mockRuleRepo.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync(new Domain.Entities.PronunciationRule());
            
            Domain.Entities.PronunciationExample captured = null;
            _mockRepo.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.PronunciationExample>()))
                     .Callback<Domain.Entities.PronunciationExample>(x => captured = x);

            var command = new CreatePronunciationExampleCommand { PronunciationRuleId = "R1", UserId = "Actor" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            captured.CreateBy.Should().Be("Actor");

            QACollector.LogTestCase("Pronunciation Example - Create", new TestCaseDetail
            {
                FunctionGroup = "CreatePronunciationExampleCommandHandler",
                TestCaseID = "CreatePronunciationExampleCommandHandler_03",
                Description = "Audit user strings match parameter binding",
                ExpectedResult = "Correct CreateBy attribute",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Audit creation tracks" }
            });
        }

        // CreatePronunciationExampleCommandHandler_04 | N | Invokes DB Operations Correctly
        [Fact]
        public async Task Handle_InvokesContextSaveChanges_Correctly()
        {
            _mockRuleRepo.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync(new Domain.Entities.PronunciationRule());

            var command = new CreatePronunciationExampleCommand { PronunciationRuleId = "R1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            _mockRepo.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.PronunciationExample>()), Times.Once);
            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Pronunciation Example - Create", new TestCaseDetail
            {
                FunctionGroup = "CreatePronunciationExampleCommandHandler",
                TestCaseID = "CreatePronunciationExampleCommandHandler_04",
                Description = "Persistence mechanics execute unconditionally on correct validations",
                ExpectedResult = "Save execution verified",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Verified contexts" }
            });
        }

        // CreatePronunciationExampleCommandHandler_05 | N | Full Properties Passed Perfectly
        [Fact]
        public async Task Handle_FullProperties_SucceedsValid()
        {
            _mockRuleRepo.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync(new Domain.Entities.PronunciationRule());

            Domain.Entities.PronunciationExample captured = null;
            _mockRepo.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.PronunciationExample>()))
                     .Callback<Domain.Entities.PronunciationExample>(x => captured = x);

            var command = new CreatePronunciationExampleCommand 
            { 
                PronunciationRuleId = "R1", TargetScript = "A", RawScript = "A_row", AudioUrl = "mp3", 
                PhoneticScript = "Aa", Meaning = "Mnn", SortOrder = 1
            };
            var result = await _handler.Handle(command, CancellationToken.None);

            captured.TargetScript.Should().Be("A");
            captured.RawScript.Should().Be("A_row");
            captured.AudioUrl.Should().Be("mp3");

            QACollector.LogTestCase("Pronunciation Example - Create", new TestCaseDetail
            {
                FunctionGroup = "CreatePronunciationExampleCommandHandler",
                TestCaseID = "CreatePronunciationExampleCommandHandler_05",
                Description = "All complex domain variables parse tightly filling structure variables flawlessly",
                ExpectedResult = "Successful wide mapping",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Complete model setup" }
            });
        }
        
        // CreatePronunciationExampleCommandHandler_06 | N | Null Audio Handled (Graceful Empty Strings allowed depending on model)
        [Fact]
        public async Task Handle_OptionalFields_ProceedsSafely()
        {
            _mockRuleRepo.Setup(x => x.GetByIdAsync("R1")).ReturnsAsync(new Domain.Entities.PronunciationRule());

            var command = new CreatePronunciationExampleCommand { PronunciationRuleId = "R1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201); // Created

            QACollector.LogTestCase("Pronunciation Example - Create", new TestCaseDetail
            {
                FunctionGroup = "CreatePronunciationExampleCommandHandler",
                TestCaseID = "CreatePronunciationExampleCommandHandler_06",
                Description = "Accepts barebones partial structures letting fluent validator manage data validation previously",
                ExpectedResult = "Success Code 201",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Null properties" }
            });
        }
    }
}
