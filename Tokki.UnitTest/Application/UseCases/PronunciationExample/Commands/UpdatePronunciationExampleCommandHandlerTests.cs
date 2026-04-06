using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationExample.Commands.UpdatePronunciationExample;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.PronunciationExample.Commands
{
    public class UpdatePronunciationExampleCommandHandlerTests
    {
        private readonly Mock<IPronunciationExampleRepository> _mockRepo;
        private readonly UpdatePronunciationExampleCommandHandler _handler;

        public UpdatePronunciationExampleCommandHandlerTests()
        {
            _mockRepo = new Mock<IPronunciationExampleRepository>();
            _handler = new UpdatePronunciationExampleCommandHandler(_mockRepo.Object);
        }

        // TC-PRN-UPE-01 | A | NotFound -> 404
        [Fact]
        public async Task Handle_NotFound_Returns404()
        {
            _mockRepo.Setup(x => x.GetByIdAsync("E1")).ReturnsAsync((Domain.Entities.PronunciationExample)null);

            var command = new UpdatePronunciationExampleCommand { ExampleId = "E1" };
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Pronunciation Example - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdatePronunciationExampleCommandHandler",
                TestCaseID = "TC-PRN-UPE-01",
                Description = "Safely guards modification blocks returning null failures mapping frontend accurately",
                ExpectedResult = "404 code",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Db entity not found" }
            });
        }

        // TC-PRN-UPE-02 | N | Skips AudioUrl update when Null
        [Fact]
        public async Task Handle_AudioNull_IgnoresUpdate()
        {
            var entity = new Domain.Entities.PronunciationExample { AudioUrl = "OldAudio" };
            _mockRepo.Setup(x => x.GetByIdAsync("E1")).ReturnsAsync(entity);

            var command = new UpdatePronunciationExampleCommand { ExampleId = "E1", AudioUrl = null };
            var result = await _handler.Handle(command, CancellationToken.None);

            entity.AudioUrl.Should().Be("OldAudio");

            QACollector.LogTestCase("Pronunciation Example - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdatePronunciationExampleCommandHandler",
                TestCaseID = "TC-PRN-UPE-02",
                Description = "Restricts wiping old data paths replacing media blocks using simple string.IsNullOrEmpty checks",
                ExpectedResult = "Audio url stays old",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Supplied audio is null" }
            });
        }

        // TC-PRN-UPE-03 | N | Valid updates mapped parameters accurately
        [Fact]
        public async Task Handle_ValidPartial_SetsNewStrings()
        {
            var entity = new Domain.Entities.PronunciationExample { TargetScript = "A" };
            _mockRepo.Setup(x => x.GetByIdAsync("E2")).ReturnsAsync(entity);

            var command = new UpdatePronunciationExampleCommand 
            { 
                ExampleId = "E2", TargetScript = "B", RawScript = "B1", 
                Meaning = "MM"
            };
            var result = await _handler.Handle(command, CancellationToken.None);

            entity.TargetScript.Should().Be("B");
            entity.RawScript.Should().Be("B1");
            entity.Meaning.Should().Be("MM");

            QACollector.LogTestCase("Pronunciation Example - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdatePronunciationExampleCommandHandler",
                TestCaseID = "TC-PRN-UPE-03",
                Description = "Replaces data appropriately formatting updates flawlessly",
                ExpectedResult = "Values matching request context",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Update text contents" }
            });
        }


        // TC-PRN-UPE-04 | N | Valid updates Audio when provided string
        [Fact]
        public async Task Handle_ValidAudio_SetsNewString()
        {
            var entity = new Domain.Entities.PronunciationExample { AudioUrl = "A.mp3" };
            _mockRepo.Setup(x => x.GetByIdAsync("E2")).ReturnsAsync(entity);

            var command = new UpdatePronunciationExampleCommand 
            { 
                ExampleId = "E2", AudioUrl = "B.mp3"
            };
            var result = await _handler.Handle(command, CancellationToken.None);

            entity.AudioUrl.Should().Be("B.mp3");

            QACollector.LogTestCase("Pronunciation Example - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdatePronunciationExampleCommandHandler",
                TestCaseID = "TC-PRN-UPE-04",
                Description = "Replaces audio file correctly if supplied path breaks limits structurally",
                ExpectedResult = "Values audio replaced",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Update media paths" }
            });
        }

        // TC-PRN-UPE-05 | N | Verifies Database Invocation Updates Ensure
        [Fact]
        public async Task Handle_AlwaysInvokesUpdate_SucceedsValid()
        {
            var entity = new Domain.Entities.PronunciationExample ();
            _mockRepo.Setup(x => x.GetByIdAsync("E2")).ReturnsAsync(entity);

            var command = new UpdatePronunciationExampleCommand { ExampleId = "E2" };
            var result = await _handler.Handle(command, CancellationToken.None);

            _mockRepo.Verify(x => x.UpdateAsync(entity), Times.Once);

            QACollector.LogTestCase("Pronunciation Example - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdatePronunciationExampleCommandHandler",
                TestCaseID = "TC-PRN-UPE-05",
                Description = "Confirms Update execution ensures state mechanics commit physically",
                ExpectedResult = "Invokes once execution mapped",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invokes update execution method" }
            });
        }

        // TC-PRN-UPE-06 | N | Verifies Database Save Changes Updates Guarantee
        [Fact]
        public async Task Handle_AlwaysInvokesSave_SucceedsValid()
        {
            var entity = new Domain.Entities.PronunciationExample ();
            _mockRepo.Setup(x => x.GetByIdAsync("E2")).ReturnsAsync(entity);

            var command = new UpdatePronunciationExampleCommand { ExampleId = "E2" };
            var result = await _handler.Handle(command, CancellationToken.None);

            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Pronunciation Example - Update", new TestCaseDetail
            {
                FunctionGroup = "UpdatePronunciationExampleCommandHandler",
                TestCaseID = "TC-PRN-UPE-06",
                Description = "Confirms saving context completes transaction securely flushing entity locks",
                ExpectedResult = "Invokes once transaction mapped",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Invokes Saving methods context correctly" }
            });
        }
    }
}
