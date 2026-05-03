using FluentAssertions;
using Hangfire;
using Moq;
using Tokki.Application.UseCases.TopikWriting.Question51.Commands;
using Tokki.Application.UseCases.TopikWriting.Question51.DTOs;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.TopikWriting
{
    public class SolveQuestion51HandlerTests
    {
        private SolveQuestion51Handler CreateHandler(Mock<IBackgroundJobClient>? mockJobs = null)
            => new SolveQuestion51Handler(
                (mockJobs ?? MockBackgroundJobClient.GetMock()).Object);

        // ── Writing_Grading_01 ─────────────────────────────────────────────
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn202WithGradingStatus()
        {
            // Arrange
            var command = new SolveQuestion51Command
            {
                Payload = new Question51RequestDto
                {
                    UserExamWritingAnswerId = "ANS-001"
                }
            };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(202);
            result.Data!.Score.Should().Be(-1);
            result.Data.Feedback.GetProperty("status").GetString().Should().Be("grading");

            QACollector.LogTestCase("TopikWriting", new TestCaseDetail
            {
                FunctionGroup = "Writing Grading",
                TestCaseID = "Writing_Grading_01",
                Description = "Submit question 51 validly → enqueue job successfully",
                ExpectedResult = "Returns 202, Score = -1, status = grading",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "UserExamWritingAnswerId is valid",
                    "Hangfire enqueue successful"
                }
            });
        }

        // ── Writing_Grading_02 ─────────────────────────────────────────────
        [Fact]
        public async Task Handle_HangfireThrowsException_ShouldReturn500()
        {
            // Arrange
            var command = new SolveQuestion51Command
            {
                Payload = new Question51RequestDto
                {
                    UserExamWritingAnswerId = "ANS-001"
                }
            };
            var handler = CreateHandler(MockBackgroundJobClient.GetThrowingMock());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Message.Should().Contain("Error processing sentence 51");

            QACollector.LogTestCase("TopikWriting", new TestCaseDetail
            {
                FunctionGroup = "Writing Grading",
                TestCaseID = "Writing_Grading_02",
                Description = "Hangfire throws exception → returns 500",
                ExpectedResult = "IsSuccess = false, StatusCode = 500, Message contains 'Error processing sentence 51'",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Hangfire.Create() throws exception",
                    "Handler catches exception in try/catch"
                }
            });
        }

        // ── Writing_Grading_03 ─────────────────────────────────────────────
        [Fact]
        public async Task Handle_EmptyAnswerId_ShouldStillEnqueueAndReturn202()
        {
            // Arrange — handler không validate, validation ở tầng trên
            var command = new SolveQuestion51Command
            {
                Payload = new Question51RequestDto
                {
                    UserExamWritingAnswerId = ""
                }
            };
            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(202);
            result.Data!.Score.Should().Be(-1);

            QACollector.LogTestCase("TopikWriting", new TestCaseDetail
            {
                FunctionGroup = "Writing Grading",
                TestCaseID = "Writing_Grading_03",
                Description = "UserExamWritingAnswerId is empty → handler does not validate, still enqueues",
                ExpectedResult = "Returns 202 because the handler has no validation logic",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "UserExamWritingAnswerId = empty string",
                    "Handler does not have validation logic"
                }
            });
        }
    }
}