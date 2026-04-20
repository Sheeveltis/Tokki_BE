using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.LiveChat.Queries.GetPendingSupports;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.LiveChat
{
    public class GetPendingSupportsQueryHandlerTests
    {
        private static GetPendingSupportsQueryHandler CreateHandler(Mock<IChatRoomRepository>? repo = null)
        {
            return new GetPendingSupportsQueryHandler((repo ?? new Mock<IChatRoomRepository>()).Object);
        }

        // GetPendingSupports_01 | N | Repository empty
        [Fact]
        public async Task Handle_NoPendingRooms_ShouldReturnEmptyList()
        {
            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetPendingSupportRoomsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatRoom>());

            var result = await CreateHandler(mockRepo).Handle(new GetPendingSupportsQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("LiveChat - Get Pending Supports", new TestCaseDetail
            {
                FunctionGroup = "GetPendingSupports", TestCaseID = "GetPendingSupports_01",
                Description = "Safely returns bound mapping ignoring empty sets efficiently",
                ExpectedResult = "Return 200 Empty List DTO", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetPendingSupportRoomsAsync parses securely empty return" }
            });
        }

        // GetPendingSupports_02 | N | FullName and Avatar mapped successfully
        [Fact]
        public async Task Handle_CustomerExists_ShouldMapFullNameAndAvatar()
        {
            var room = new ChatRoom { ChatRoomId = "R1", CreatedAt = DateTime.UtcNow, Members = new List<ChatRoomMember> { new() { User = new Account { FullName = "Valid User", AvatarUrl = "v.jpg" } } } };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetPendingSupportRoomsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatRoom> { room });

            var result = await CreateHandler(mockRepo).Handle(new GetPendingSupportsQuery(), CancellationToken.None);

            result.Data[0].RoomName.Should().Be("Valid User");
            result.Data[0].RoomAvatar.Should().Be("v.jpg");
            result.Data[0].IsSupport.Should().BeTrue();

            QACollector.LogTestCase("LiveChat - Get Pending Supports", new TestCaseDetail
            {
                FunctionGroup = "GetPendingSupports", TestCaseID = "GetPendingSupports_02",
                Description = "If first member references valid customer details strings map through explicitly",
                ExpectedResult = "Return 200 properly bound variables mapped properly", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "customer != null" }
            });
        }

        // GetPendingSupports_03 | N | FirstOrDefault resolves to null -> Khách ẩn danh
        [Fact]
        public async Task Handle_CustomerNull_ShouldFallbackToAnonymousString()
        {
            var room = new ChatRoom { ChatRoomId = "R1", CreatedAt = DateTime.UtcNow, Members = new List<ChatRoomMember>() };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetPendingSupportRoomsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatRoom> { room });

            var result = await CreateHandler(mockRepo).Handle(new GetPendingSupportsQuery(), CancellationToken.None);

            result.Data[0].RoomName.Should().Be("Khách ẩn danh");
            result.Data[0].RoomAvatar.Should().Be("");

            QACollector.LogTestCase("LiveChat - Get Pending Supports", new TestCaseDetail
            {
                FunctionGroup = "GetPendingSupports", TestCaseID = "GetPendingSupports_03",
                Description = "Members list yields null mapping defaults safely against UI constraints",
                ExpectedResult = "Return 200 Default Fallback Value Set", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "r.Members.FirstOrDefault()?.User == null" }
            });
        }

        // GetPendingSupports_04 | N | User exists but FullName is null -> Khách ẩn danh
        [Fact]
        public async Task Handle_UserFullNameNull_ShouldFallbackToAnonymousString()
        {
            var room = new ChatRoom { ChatRoomId = "R1", CreatedAt = DateTime.UtcNow, Members = new List<ChatRoomMember> { new() { User = new Account { FullName = null, AvatarUrl = null } } } };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetPendingSupportRoomsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatRoom> { room });

            var result = await CreateHandler(mockRepo).Handle(new GetPendingSupportsQuery(), CancellationToken.None);

            result.Data[0].RoomName.Should().Be("Khách ẩn danh");
            result.Data[0].RoomAvatar.Should().Be("");

            QACollector.LogTestCase("LiveChat - Get Pending Supports", new TestCaseDetail
            {
                FunctionGroup = "GetPendingSupports", TestCaseID = "GetPendingSupports_04",
                Description = "Missing database profile string property binds properly through fallback syntax",
                ExpectedResult = "Return 200, handles ?? operator correctly", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "customer?.FullName ?? null triggers fallback" }
            });
        }

        // GetPendingSupports_05 | N | Multiple logic processing
        [Fact]
        public async Task Handle_MultipleRooms_ShouldProcessIteratively()
        {
            var room1 = new ChatRoom { ChatRoomId = "R1", CreatedAt = DateTime.UtcNow, Members = new List<ChatRoomMember> { new() { User = new Account { FullName = "Valid User", AvatarUrl = "v.jpg" } } } };
            var room2 = new ChatRoom { ChatRoomId = "R2", CreatedAt = DateTime.UtcNow, Members = new List<ChatRoomMember>() };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetPendingSupportRoomsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatRoom> { room1, room2 });

            var result = await CreateHandler(mockRepo).Handle(new GetPendingSupportsQuery(), CancellationToken.None);

            result.Data.Should().HaveCount(2);

            QACollector.LogTestCase("LiveChat - Get Pending Supports", new TestCaseDetail
            {
                FunctionGroup = "GetPendingSupports", TestCaseID = "GetPendingSupports_05",
                Description = "Validates comprehensive multi-element transformation",
                ExpectedResult = "Return 200, successfully iterates mapping enumerator", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "LINQ Select projects comprehensively across list sizes" }
            });
        }

        // GetPendingSupports_06 | A | Repository error propagates Native execution stack
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateExceptionNatively()
        {
            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetPendingSupportRoomsAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("EF Locked"));

            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo).Handle(new GetPendingSupportsQuery(), CancellationToken.None));

            QACollector.LogTestCase("LiveChat - Get Pending Supports", new TestCaseDetail
            {
                FunctionGroup = "GetPendingSupports", TestCaseID = "GetPendingSupports_06",
                Description = "Unhandled context issues throws effectively across generic mapping execution flow",
                ExpectedResult = "Throws Exception internally propagating upwards to execution limits", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "EF Core database exceptions trace properly" }
            });
        }
    }
}
