using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.LiveChat.Queries.GetMyRooms;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.LiveChat
{
    public class GetMyRoomsQueryHandlerTests
    {
        private static GetMyRoomsQueryHandler CreateHandler(Mock<IChatRoomRepository>? repo = null)
        {
            return new GetMyRoomsQueryHandler((repo ?? new Mock<IChatRoomRepository>()).Object);
        }

        // TC-LCH-GMR-01 | N | Empty Rooms list
        [Fact]
        public async Task Handle_EmptyRooms_ShouldReturnEmptyDTOList()
        {
            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetUserRoomsAsync("U1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatRoom>());

            var query = new GetMyRoomsQuery { UserId = "U1" };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("LiveChat - Get My Rooms", new TestCaseDetail
            {
                FunctionGroup = "GetMyRooms", TestCaseID = "TC-LCH-GMR-01",
                Description = "Repository returns empty List mapping securely",
                ExpectedResult = "Return 200 Empty List DTO", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "rooms loop skips safely" }
            });
        }

        // TC-LCH-GMR-02 | N | Room is Group and NOT Support -> Uses Room Name correctly
        [Fact]
        public async Task Handle_GroupNotSupport_ShouldUseRoomNameAndNoAvatar()
        {
            var room = new ChatRoom { ChatRoomId = "R1", Name = "Team Chat", IsGroup = true, IsSupport = false, Members = new List<ChatRoomMember>() };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetUserRoomsAsync("U1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatRoom> { room });

            var query = new GetMyRoomsQuery { UserId = "U1" };
            var result = await CreateHandler(mockRepo).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data[0].RoomName.Should().Be("Team Chat");
            result.Data[0].RoomAvatar.Should().BeEmpty();

            QACollector.LogTestCase("LiveChat - Get My Rooms", new TestCaseDetail
            {
                FunctionGroup = "GetMyRooms", TestCaseID = "TC-LCH-GMR-02",
                Description = "General group sets condition mapping directly resolving string explicitly mapping room avatar safely",
                ExpectedResult = "Return 200, checks bindings", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "if (!room.IsGroup || room.IsSupport) is false" }
            });
        }

        // TC-LCH-GMR-03 | N | Room is Not Group -> Use Other Member Info
        [Fact]
        public async Task Handle_NotGroup_ShouldUseOtherMemberInfo()
        {
            var otherMember = new ChatRoomMember { UserId = "U2", User = new Account { FullName = "Bob Ross", AvatarUrl = "bob.jpg" } };
            var room = new ChatRoom { ChatRoomId = "R1", IsGroup = false, Members = new List<ChatRoomMember> { new() { UserId = "U1" }, otherMember } };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetUserRoomsAsync("U1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatRoom> { room });

            var result = await CreateHandler(mockRepo).Handle(new GetMyRoomsQuery { UserId = "U1" }, CancellationToken.None);

            result.Data[0].RoomName.Should().Be("Bob Ross");
            result.Data[0].RoomAvatar.Should().Be("bob.jpg");

            QACollector.LogTestCase("LiveChat - Get My Rooms", new TestCaseDetail
            {
                FunctionGroup = "GetMyRooms", TestCaseID = "TC-LCH-GMR-03",
                Description = "Private chat pulls other members detailed account parsing it down to display layer",
                ExpectedResult = "Return 200 Display name binds other member info securely", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "otherMember parsed successfully" }
            });
        }

        // TC-LCH-GMR-04 | N | Room is Support -> Use Other Member Info appropriately
        [Fact]
        public async Task Handle_IsSupport_ShouldUseOtherMemberInfo()
        {
            var otherMember = new ChatRoomMember { UserId = "U2", User = new Account { FullName = "Supporter", AvatarUrl = "sup.jpg" } };
            // Group chat but IsSupport = true
            var room = new ChatRoom { ChatRoomId = "R1", IsGroup = true, IsSupport = true, Members = new List<ChatRoomMember> { new() { UserId = "U1" }, otherMember } };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetUserRoomsAsync("U1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatRoom> { room });

            var result = await CreateHandler(mockRepo).Handle(new GetMyRoomsQuery { UserId = "U1" }, CancellationToken.None);

            result.Data[0].RoomName.Should().Be("Supporter");

            QACollector.LogTestCase("LiveChat - Get My Rooms", new TestCaseDetail
            {
                FunctionGroup = "GetMyRooms", TestCaseID = "TC-LCH-GMR-04",
                Description = "Logical OR branch allows IsSupport=true to pull specific member profile rendering",
                ExpectedResult = "Return 200, RoomName mapped cleanly", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "room.IsSupport executes if branch" }
            });
        }

        // TC-LCH-GMR-05 | N | Missing other member fallback (Support) -> Default Null Coalesce Room Name
        [Fact]
        public async Task Handle_MissingOtherMember_ShouldFallbackToGenericRoomName()
        {
            // Only Requesting User is in the room
            var room = new ChatRoom { ChatRoomId = "R1", IsGroup = false, Members = new List<ChatRoomMember> { new() { UserId = "U1" } }, Name = null };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetUserRoomsAsync("U1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatRoom> { room });

            var result = await CreateHandler(mockRepo).Handle(new GetMyRoomsQuery { UserId = "U1" }, CancellationToken.None);

            result.Data[0].RoomName.Should().Be("Phòng chat");
            
            QACollector.LogTestCase("LiveChat - Get My Rooms", new TestCaseDetail
            {
                FunctionGroup = "GetMyRooms", TestCaseID = "TC-LCH-GMR-05",
                Description = "Fallback condition tests missing members default string null coalescing functionality",
                ExpectedResult = "Return 200 with const generic fallback room descriptor", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "room.Name ?? 'Phòng chat'" }
            });
        }

        // TC-LCH-GMR-06 | A | Repostory throws unhandled exception -> Propagate Upwards
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetUserRoomsAsync("U1", It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Network lost"));

            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo).Handle(new GetMyRoomsQuery { UserId = "U1" }, CancellationToken.None));

            QACollector.LogTestCase("LiveChat - Get My Rooms", new TestCaseDetail
            {
                FunctionGroup = "GetMyRooms", TestCaseID = "TC-LCH-GMR-06",
                Description = "Testing repository global failure natively throws unmapped Exception outwards",
                ExpectedResult = "Throws Exception internally mapped", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Propagate to MediatR root natively" }
            });
        }
    }
}
