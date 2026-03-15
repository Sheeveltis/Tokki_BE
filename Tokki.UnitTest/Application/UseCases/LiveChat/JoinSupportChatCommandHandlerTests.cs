using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.LiveChat.Commands.JoinSupportChat;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.LiveChat
{
    public class JoinSupportChatCommandHandlerTests
    {
        private JoinSupportChatCommandHandler CreateHandler(
            Mock<IChatRoomRepository>? chatRoomRepo = null,
            Mock<IChatRepository>? chatRepo = null,
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IChatNotificationService>? notifier = null)
        {
            var mockChatRepo = chatRepo ?? new Mock<IChatRepository>();
            mockChatRepo.Setup(x => x.CreateMessageAsync(It.IsAny<ChatMessage>()))
                        .Returns(Task.CompletedTask);

            return new JoinSupportChatCommandHandler(
                (chatRoomRepo ?? new Mock<IChatRoomRepository>()).Object,
                mockChatRepo.Object,
                (accountRepo ?? MockAccountRepository.GetMock()).Object,
                MockIdGeneratorService.GetMock().Object,
                (notifier ?? new Mock<IChatNotificationService>()).Object);
        }

        [Fact]
        public async Task Handle_RoomNotFound_ShouldReturn404()
        {
            var command = new JoinSupportChatCommand
            {
                RoomId = "ROOM-INVALID",
                StaffId = "STAFF-001"
            };

            var mockRoomRepo = new Mock<IChatRoomRepository>();
            mockRoomRepo.Setup(x => x.GetRoomByIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ChatRoom?)null);

            var handler = CreateHandler(chatRoomRepo: mockRoomRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("LiveChat - Join Support Chat", new TestCaseDetail
            {
                FunctionGroup = "Join Support Chat",
                TestCaseID = "TC-CHAT-JON-01",
                Description = "Join chat với RoomId không tồn tại",
                ExpectedResult = "Return 404 ChatRoomNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid RoomId",
                    "Return 404"
                }
            });
        }

        [Fact]
        public async Task Handle_RoomAlreadyHasOtherStaff_ShouldReturn400()
        {
            var command = new JoinSupportChatCommand
            {
                RoomId = "ROOM-001",
                StaffId = "STAFF-002"
            };

            // Room đã có 2 members (user + staff khác)
            var room = new ChatRoom
            {
                ChatRoomId = "ROOM-001",
                Members = new List<ChatRoomMember>
                {
                    new ChatRoomMember { UserId = "USER-001" },
                    new ChatRoomMember { UserId = "STAFF-001" } // staff khác
                }
            };

            var mockRoomRepo = new Mock<IChatRoomRepository>();
            mockRoomRepo.Setup(x => x.GetRoomByIdAsync(
                        "ROOM-001",
                        It.IsAny<CancellationToken>()))
                        .ReturnsAsync(room);

            var handler = CreateHandler(chatRoomRepo: mockRoomRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);

            QACollector.LogTestCase("LiveChat - Join Support Chat", new TestCaseDetail
            {
                FunctionGroup = "Join Support Chat",
                TestCaseID = "TC-CHAT-JON-02",
                Description = "Room đã có staff khác → return 400 ChatRoomAlreadySupported",
                ExpectedResult = "Return 400 ChatRoomAlreadySupported",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Room.Members.Count > 1",
                    "Staff khác đang hỗ trợ",
                    "Return 400"
                }
            });
        }

        [Fact]
        public async Task Handle_StaffAlreadyInRoom_ShouldReturnIdempotent200()
        {
            var command = new JoinSupportChatCommand
            {
                RoomId = "ROOM-001",
                StaffId = "STAFF-001"
            };

            // Staff đã trong room rồi
            var room = new ChatRoom
            {
                ChatRoomId = "ROOM-001",
                Members = new List<ChatRoomMember>
                {
                    new ChatRoomMember { UserId = "USER-001" },
                    new ChatRoomMember { UserId = "STAFF-001" } // đã join
                }
            };

            var mockRoomRepo = new Mock<IChatRoomRepository>();
            mockRoomRepo.Setup(x => x.GetRoomByIdAsync(
                        "ROOM-001",
                        It.IsAny<CancellationToken>()))
                        .ReturnsAsync(room);

            var handler = CreateHandler(chatRoomRepo: mockRoomRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            QACollector.LogTestCase("LiveChat - Join Support Chat", new TestCaseDetail
            {
                FunctionGroup = "Join Support Chat",
                TestCaseID = "TC-CHAT-JON-03",
                Description = "Staff đã trong room → idempotent, return 200",
                ExpectedResult = "Return Success = true (idempotent)",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Staff đã là member (boundary: đã join)",
                    "Idempotent → return true"
                }
            });
        }
    }
}