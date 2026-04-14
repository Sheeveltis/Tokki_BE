using FluentAssertions;
using Hangfire;
using Moq;
using Tokki.Application.UseCases.TopikWriting.Question53.Commands;
using Tokki.Application.UseCases.TopikWriting.Question53.DTOs;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.TopikWriting
{
    public class SolveQuestion53HandlerTests
    {
        private SolveQuestion53Handler CreateHandler(Mock<IBackgroundJobClient>? mockJobs = null)
            => new SolveQuestion53Handler(
                (mockJobs ?? MockBackgroundJobClient.GetMock()).Object);

        // ── TC-WRT53-01 ─────────────────────────────────────────────
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturn202WithGradingStatus()
        {
            // Arrange
            var command = new SolveQuestion53Command
            {
                Payload = new Question53RequestDto
                {
                    UserExamWritingAnswerId = "ANS-003"
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
                TestCaseID = "TC-WRT53-01",
                Description = "Submit question 53 validly → enqueue job successfully",
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

        // ── TC-WRT53-02 ─────────────────────────────────────────────
        [Fact]
        public async Task Handle_HangfireThrowsException_ShouldReturn500()
        {
            // Arrange
            var command = new SolveQuestion53Command
            {
                Payload = new Question53RequestDto
                {
                    UserExamWritingAnswerId = "ANS-003"
                }
            };
            var handler = CreateHandler(MockBackgroundJobClient.GetThrowingMock());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(500);
            result.Message.Should().Contain("Error processing sentence 53");

            QACollector.LogTestCase("TopikWriting", new TestCaseDetail
            {
                FunctionGroup = "Writing Grading",
                TestCaseID = "TC-WRT53-02",
                Description = "Hangfire throws exception → returns 500",
                ExpectedResult = "IsSuccess = false, StatusCode = 500, Message contains 'Error processing sentence 53'",
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

        // ── TC-WRT53-03 ─────────────────────────────────────────────
        [Fact]
        public async Task Handle_EmptyAnswerId_ShouldStillEnqueueAndReturn202()
        {
            // Arrange
            var command = new SolveQuestion53Command
            {
                Payload = new Question53RequestDto
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
                TestCaseID = "TC-WRT53-03",
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