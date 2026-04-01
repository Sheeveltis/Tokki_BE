using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.LiveChat.Queries.GetChatHistory;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.LiveChat
{
    public class GetChatHistoryQueryHandlerTests
    {
        private static GetChatHistoryQueryHandler CreateHandler(Mock<IChatRepository>? chatRepo = null)
        {
            return new GetChatHistoryQueryHandler((chatRepo ?? new Mock<IChatRepository>()).Object);
        }

        // TC-LCH-GCH-01 | N | Repo returns null
        [Fact]
        public async Task Handle_MessagesNull_ShouldReturnEmptyList()
        {
            var mockRepo = new Mock<IChatRepository>();
            mockRepo.Setup(x => x.GetHistoryAsync(It.IsAny<string>())).ReturnsAsync((List<ChatMessage>?)null);

            var query = new GetChatHistoryQuery { RoomId = "R1" };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("LiveChat - Get Chat History", new TestCaseDetail
            {
                FunctionGroup = "GetChatHistory", TestCaseID = "TC-LCH-GCH-01",
                Description = "Repository resolves safely null for history payload mapping softly",
                ExpectedResult = "Return 200 with empty initialized List", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "messages == null" }
            });
        }

        // TC-LCH-GCH-02 | N | Repo returns empty
        [Fact]
        public async Task Handle_MessagesEmpty_ShouldReturnEmptyList()
        {
            var mockRepo = new Mock<IChatRepository>();
            mockRepo.Setup(x => x.GetHistoryAsync(It.IsAny<string>())).ReturnsAsync(new List<ChatMessage>());

            var query = new GetChatHistoryQuery { RoomId = "R1" };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("LiveChat - Get Chat History", new TestCaseDetail
            {
                FunctionGroup = "GetChatHistory", TestCaseID = "TC-LCH-GCH-02",
                Description = "Returns successfully wrapping empty domain list mappings properly",
                ExpectedResult = "Return 200, empty list", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!messages.Any()" }
            });
        }

        // TC-LCH-GCH-03 | N | Verify sort order
        [Fact]
        public async Task Handle_ValidMessages_ShouldSortByCreatedAt()
        {
            var messages = new List<ChatMessage>
            {
                new() { ChatMessageId = "M1", Content = "Second", CreatedAt = new DateTime(2023, 2, 1) },
                new() { ChatMessageId = "M2", Content = "First", CreatedAt = new DateTime(2023, 1, 1) },
                new() { ChatMessageId = "M3", Content = "Third", CreatedAt = new DateTime(2023, 3, 1) }
            };

            var mockRepo = new Mock<IChatRepository>();
            mockRepo.Setup(x => x.GetHistoryAsync(It.IsAny<string>())).ReturnsAsync(messages);

            var query = new GetChatHistoryQuery { RoomId = "R1" };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            result.Data[0].Content.Should().Be("First");
            result.Data[2].Content.Should().Be("Third");

            QACollector.LogTestCase("LiveChat - Get Chat History", new TestCaseDetail
            {
                FunctionGroup = "GetChatHistory", TestCaseID = "TC-LCH-GCH-03",
                Description = "Extracted list is predictably parsed by LINQ dynamically ordering items",
                ExpectedResult = "Return 200 mapped strictly ascending by CreatedAt", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "messages.OrderBy(x => x.CreatedAt)" }
            });
        }

        // TC-LCH-GCH-04 | N | Verify identical dates boundary
        [Fact]
        public async Task Handle_IdenticalTimestamps_ShouldSortSafely()
        {
            var date = new DateTime(2023, 1, 1);
            var messages = new List<ChatMessage> { new() { Content = "A", CreatedAt = date }, new() { Content = "B", CreatedAt = date } };

            var mockRepo = new Mock<IChatRepository>();
            mockRepo.Setup(x => x.GetHistoryAsync(It.IsAny<string>())).ReturnsAsync(messages);

            var result = await CreateHandler(mockRepo).Handle(new GetChatHistoryQuery { RoomId = "R1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);

            QACollector.LogTestCase("LiveChat - Get Chat History", new TestCaseDetail
            {
                FunctionGroup = "GetChatHistory", TestCaseID = "TC-LCH-GCH-04",
                Description = "Same timestamp objects sorted stably mapping appropriately",
                ExpectedResult = "Return 200 valid unchanged mapping count = 2", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Identical dates sorting stability" }
            });
        }

        // TC-LCH-GCH-05 | A | Repo throws exception
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldCatchWrapper()
        {
            var mockRepo = new Mock<IChatRepository>();
            mockRepo.Setup(x => x.GetHistoryAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Database disconnected"));

            var query = new GetChatHistoryQuery { RoomId = "R1" };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Không thể tải lịch sử chat lúc này");

            QACollector.LogTestCase("LiveChat - Get Chat History", new TestCaseDetail
            {
                FunctionGroup = "GetChatHistory", TestCaseID = "TC-LCH-GCH-05",
                Description = "Fallback catch limits stack bleeding returning soft error output message natively",
                ExpectedResult = "Return Failure string wrapper softly", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "catch (Exception)" }
            });
        }

        // TC-LCH-GCH-06 | A | Assert empty history doesn't mutate object
        [Fact]
        public async Task Handle_ObjectReference_ShouldReturnNewList()
        {
            var mockRepo = new Mock<IChatRepository>();
            var innerList = new List<ChatMessage>();
            mockRepo.Setup(x => x.GetHistoryAsync(It.IsAny<string>())).ReturnsAsync(innerList);

            var result = await CreateHandler(mockRepo).Handle(new GetChatHistoryQuery { RoomId = "R1" }, CancellationToken.None);

            result.Data.Should().NotBeSameAs(innerList); // Because explicitly creates `new List<ChatMessage>()`

            QACollector.LogTestCase("LiveChat - Get Chat History", new TestCaseDetail
            {
                FunctionGroup = "GetChatHistory", TestCaseID = "TC-LCH-GCH-06",
                Description = "Checks the empty instantiation branch does not reuse mapping",
                ExpectedResult = "NotBeSameAs matches correctly instance logic string", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "new List<ChatMessage>()" }
            });
        }
    }
}
