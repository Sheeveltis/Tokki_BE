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

        // ── TC-WRT51-01 ─────────────────────────────────────────────
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
                TestCaseID = "TC-WRT51-01",
                Description = "Submit câu 51 hợp lệ → enqueue job thành công",
                ExpectedResult = "Trả về 202, Score = -1, status = grading",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "UserExamWritingAnswerId hợp lệ",
                    "Hangfire enqueue thành công"
                }
            });
        }

        // ── TC-WRT51-02 ─────────────────────────────────────────────
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
            result.Message.Should().Contain("Lỗi xử lý câu 51");

            QACollector.LogTestCase("TopikWriting", new TestCaseDetail
            {
                FunctionGroup = "Writing Grading",
                TestCaseID = "TC-WRT51-02",
                Description = "Hangfire throw exception → trả về 500",
                ExpectedResult = "IsSuccess = false, StatusCode = 500, Message chứa 'Lỗi xử lý câu 51'",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Hangfire.Create() ném exception",
                    "Handler bắt exception trong try/catch"
                }
            });
        }

        // ── TC-WRT51-03 ─────────────────────────────────────────────
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
                TestCaseID = "TC-WRT51-03",
                Description = "UserExamWritingAnswerId rỗng → handler không validate, vẫn enqueue",
                ExpectedResult = "Trả về 202 vì handler không có validation logic",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "UserExamWritingAnswerId = empty string",
                    "Handler không có validation logic"
                }
            });
        }
    }
}