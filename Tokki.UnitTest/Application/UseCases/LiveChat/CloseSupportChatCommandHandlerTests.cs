using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.LiveChat.Commands.CloseSupportChat;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.LiveChat
{
    public class CloseSupportChatCommandHandlerTests
    {
        private static CloseSupportChatCommandHandler CreateHandler(Mock<IChatRoomRepository>? repo = null)
        {
            return new CloseSupportChatCommandHandler((repo ?? new Mock<IChatRoomRepository>()).Object);
        }

        // CloseSupportChat_01 | 404 | Room not found
        [Fact]
        public async Task Handle_RoomNull_ShouldReturnFailure()
        {
            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetRoomByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ChatRoom?)null);

            var command = new CloseSupportChatCommand { RoomId = "R1", UserId = "U1" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Phòng chat không tồn tại");

            QACollector.LogTestCase("LiveChat - Close Support", new TestCaseDetail
            {
                FunctionGroup = "CloseSupportChat", TestCaseID = "CloseSupportChat_01",
                Description = "Room cannot be resolved against existing entity sets via ID",
                ExpectedResult = "Return 404 Validation Failure", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "room == null" }
            });
        }

        // CloseSupportChat_02 | 403 | User is not member
        [Fact]
        public async Task Handle_UserNotMember_ShouldReturnFailureAuthorization()
        {
            var room = new ChatRoom { Members = new List<ChatRoomMember> { new() { UserId = "U2" } } };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetRoomByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(room);

            var command = new CloseSupportChatCommand { RoomId = "R1", UserId = "U1" }; // Requesting User is U1, Room has U2
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("không có quyền");

            QACollector.LogTestCase("LiveChat - Close Support", new TestCaseDetail
            {
                FunctionGroup = "CloseSupportChat", TestCaseID = "CloseSupportChat_02",
                Description = "User submitting request exists outside of the room's ChatMember tracking",
                ExpectedResult = "Return Failure 403", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "!room.Members.Any" }
            });
        }

        // CloseSupportChat_03 | 200 | Valid Logic updates IsClosed to true
        [Fact]
        public async Task Handle_ValidRequest_ShouldSetIsClosedTrueAndSave()
        {
            var room = new ChatRoom { Members = new List<ChatRoomMember> { new() { UserId = "U1" } }, IsClosed = false };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetRoomByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(room);

            var command = new CloseSupportChatCommand { RoomId = "R1", UserId = "U1" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            
            mockRepo.Verify(x => x.UpdateRoomAsync(It.Is<ChatRoom>(r => r.IsClosed == true)), Times.Once);
            mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("LiveChat - Close Support", new TestCaseDetail
            {
                FunctionGroup = "CloseSupportChat", TestCaseID = "CloseSupportChat_03",
                Description = "Command fully executes, flipping IsClosed to true persisting downwards",
                ExpectedResult = "Return 200 and true", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "room.IsClosed = true" }
            });
        }

        // CloseSupportChat_04 | 200 | Idempotent Close (Already closed)
        [Fact]
        public async Task Handle_RoomAlreadyClosed_ShouldIdempotentlyReturnSuccess()
        {
            var room = new ChatRoom { Members = new List<ChatRoomMember> { new() { UserId = "U1" } }, IsClosed = true };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetRoomByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(room);

            var command = new CloseSupportChatCommand { RoomId = "R1", UserId = "U1" };
            var result = await CreateHandler(mockRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            mockRepo.Verify(x => x.UpdateRoomAsync(It.IsAny<ChatRoom>()), Times.Once);

            QACollector.LogTestCase("LiveChat - Close Support", new TestCaseDetail
            {
                FunctionGroup = "CloseSupportChat", TestCaseID = "CloseSupportChat_04",
                Description = "Room already closed still goes through setting boolean gracefully acting idempotent",
                ExpectedResult = "Return 200, successfully repeats closure", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Idempotent logic verified" }
            });
        }

        // CloseSupportChat_05 | 500 | UpdateRoomThrows
        [Fact]
        public async Task Handle_UpdateRoomThrows_ShouldPropagateException()
        {
            var room = new ChatRoom { Members = new List<ChatRoomMember> { new() { UserId = "U1" } } };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetRoomByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(room);
            mockRepo.Setup(x => x.UpdateRoomAsync(It.IsAny<ChatRoom>())).ThrowsAsync(new Exception("Database Object Readonly"));

            var command = new CloseSupportChatCommand { RoomId = "R1", UserId = "U1" };
            
            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo).Handle(command, CancellationToken.None));

            QACollector.LogTestCase("LiveChat - Close Support", new TestCaseDetail
            {
                FunctionGroup = "CloseSupportChat", TestCaseID = "CloseSupportChat_05",
                Description = "Uncaught repository failure immediately exposes Exception to outer MediatR ring",
                ExpectedResult = "Throws Exception natively", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Propagate uncaught trace from implementation" }
            });
        }

        // CloseSupportChat_06 | 500 | SaveChangesThrows
        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldPropagateException()
        {
             var room = new ChatRoom { Members = new List<ChatRoomMember> { new() { UserId = "U1" } } };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.GetRoomByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(room);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("EF Conflict"));

            var command = new CloseSupportChatCommand { RoomId = "R1", UserId = "U1" };
            
            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo).Handle(command, CancellationToken.None));

            QACollector.LogTestCase("LiveChat - Close Support", new TestCaseDetail
            {
                FunctionGroup = "CloseSupportChat", TestCaseID = "CloseSupportChat_06",
                Description = "Final EF Commit errors mapping similarly outwards",
                ExpectedResult = "Throws Exception internally mapped", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Commit failure natively unhandled" }
            });
        }
    }
}
