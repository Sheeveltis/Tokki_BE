using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.LiveChat.Commands.JoinSupportChat;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.LiveChat
{
    public class JoinSupportChatCommandHandlerTests
    {
        private static JoinSupportChatCommandHandler CreateHandler(
            Mock<IChatRoomRepository>? roomRepo = null,
            Mock<IChatRepository>? chatRepo = null,
            Mock<IAccountRepository>? accountRepo = null,
            Mock<IIdGeneratorService>? idGen = null,
            Mock<IChatNotificationService>? notifier = null)
        {
            return new JoinSupportChatCommandHandler(
                (roomRepo ?? new Mock<IChatRoomRepository>()).Object,
                (chatRepo ?? new Mock<IChatRepository>()).Object,
                (accountRepo ?? new Mock<IAccountRepository>()).Object,
                (idGen ?? new Mock<IIdGeneratorService>()).Object,
                (notifier ?? new Mock<IChatNotificationService>()).Object);
        }

        // TC-LCH-JSC-01 | 404 | Room == null
        [Fact]
        public async Task Handle_RoomNotFound_ShouldReturn404()
        {
            var mockRoomRepo = new Mock<IChatRoomRepository>();
            mockRoomRepo.Setup(x => x.GetRoomByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ChatRoom?)null);

            var command = new JoinSupportChatCommand { RoomId = "R1", StaffId = "S1" };
            var result = await CreateHandler(roomRepo: mockRoomRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Be(AppErrors.ChatRoomNotFound.Description);

            QACollector.LogTestCase("LiveChat - Join Support", new TestCaseDetail
            {
                FunctionGroup = "JoinSupportChat", TestCaseID = "TC-LCH-JSC-01",
                Description = "Room lookup resolves to null, early termination returning 404 validation",
                ExpectedResult = "Return 404 AppErrors.ChatRoomNotFound", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "room == null" }
            });
        }

        // TC-LCH-JSC-02 | 400 | Room already has > 1 members and staff isn't one
        [Fact]
        public async Task Handle_RoomAlreadySupported_ShouldReturn400()
        {
            var room = new ChatRoom { Members = new List<ChatRoomMember> { new() { UserId = "U1" }, new() { UserId = "S1" } } };

            var mockRoomRepo = new Mock<IChatRoomRepository>();
            mockRoomRepo.Setup(x => x.GetRoomByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(room);

            var command = new JoinSupportChatCommand { RoomId = "R1", StaffId = "S2" }; // Requesting with S2, S1 is already there
            var result = await CreateHandler(roomRepo: mockRoomRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Be(AppErrors.ChatRoomAlreadySupported.Description);

            QACollector.LogTestCase("LiveChat - Join Support", new TestCaseDetail
            {
                FunctionGroup = "JoinSupportChat", TestCaseID = "TC-LCH-JSC-02",
                Description = "Room contains more than 1 member but requesting staff isn't already inside",
                ExpectedResult = "Return 400 AppErrors.ChatRoomAlreadySupported", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Members > 1 && !room.Members.Any(StaffId)" }
            });
        }

        // TC-LCH-JSC-03 | 200 | Room > 1 member and staff is already in -> Idempotent Success
        [Fact]
        public async Task Handle_StaffAlreadyInRoom_ShouldReturnSuccessIdempotently()
        {
            var room = new ChatRoom { Members = new List<ChatRoomMember> { new() { UserId = "U1" }, new() { UserId = "S2" } } };

            var mockRoomRepo = new Mock<IChatRoomRepository>();
            mockRoomRepo.Setup(x => x.GetRoomByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(room);

            var command = new JoinSupportChatCommand { RoomId = "R1", StaffId = "S2" };
            var result = await CreateHandler(roomRepo: mockRoomRepo).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            QACollector.LogTestCase("LiveChat - Join Support", new TestCaseDetail
            {
                FunctionGroup = "JoinSupportChat", TestCaseID = "TC-LCH-JSC-03",
                Description = "Staff already exists inside member list of >1 length, avoiding redundancy seamlessly",
                ExpectedResult = "Return 200 true logic completes explicitly idempotently", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Members > 1 && Any(StaffId) -> Return 200" }
            });
        }

        // TC-LCH-JSC-04 | 200 | Valid logic parsing name
        [Fact]
        public async Task Handle_ValidRequestWithUserInfo_ShouldParseNameAndSendNotification()
        {
            var room = new ChatRoom { Members = new List<ChatRoomMember> { new() { UserId = "U1" } } }; // Only 1 member
            var staffInfo = new AccountBasicInfoDTO { FullName = "Nguyễn Văn Staff" };

            var mockRoomRepo = new Mock<IChatRoomRepository>();
            mockRoomRepo.Setup(x => x.GetRoomByIdAsync("R1", It.IsAny<CancellationToken>())).ReturnsAsync(room);

            var mockAccRepo = new Mock<IAccountRepository>();
            mockAccRepo.Setup(x => x.GetBasicInfoAsync("S1")).ReturnsAsync(staffInfo);

            var mockId = new Mock<IIdGeneratorService>();
            mockId.Setup(x => x.Generate(15)).Returns("MEM1");
            mockId.Setup(x => x.Generate(21)).Returns("MSG1");

            var mockChatRepo = new Mock<IChatRepository>();
            var mockNotifier = new Mock<IChatNotificationService>();

            var command = new JoinSupportChatCommand { RoomId = "R1", StaffId = "S1" };
            var result = await CreateHandler(roomRepo: mockRoomRepo, accountRepo: mockAccRepo, idGen: mockId, chatRepo: mockChatRepo, notifier: mockNotifier).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            mockRoomRepo.Verify(x => x.AddMemberAsync(It.IsAny<ChatRoomMember>()), Times.Once);
            mockRoomRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Verify notification includes staff name
            mockNotifier.Verify(x => x.SendMessageToRoomAsync("R1", It.Is<ChatMessage>(m => m.Content.Contains("Nguyễn Văn Staff"))), Times.Once);

            QACollector.LogTestCase("LiveChat - Join Support", new TestCaseDetail
            {
                FunctionGroup = "JoinSupportChat", TestCaseID = "TC-LCH-JSC-04",
                Description = "Valid join successfully retrieves account full name attaching to system presence message",
                ExpectedResult = "Return 200, sends message mapped out appropriately via notification", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "AccountBase returned full name -> SystemMessage constructed" }
            });
        }

        // TC-LCH-JSC-05 | 200 | Valid logic, Account Info missing/null default text fallback
        [Fact]
        public async Task Handle_ValidRequestMissingUserInfo_ShouldUseDefaultFallback()
        {
            var room = new ChatRoom { Members = new List<ChatRoomMember> { new() { UserId = "U1" } } }; // Only 1 member

            var mockRoomRepo = new Mock<IChatRoomRepository>();
            mockRoomRepo.Setup(x => x.GetRoomByIdAsync("R1", It.IsAny<CancellationToken>())).ReturnsAsync(room);

            var mockAccRepo = new Mock<IAccountRepository>();
            mockAccRepo.Setup(x => x.GetBasicInfoAsync("S1")).ReturnsAsync((AccountBasicInfoDTO?)null); // Null info

            var mockId = new Mock<IIdGeneratorService>();
            mockId.Setup(x => x.Generate(It.IsAny<int>())).Returns("ID1");

            var mockChatRepo = new Mock<IChatRepository>();
            var mockNotifier = new Mock<IChatNotificationService>();

            var command = new JoinSupportChatCommand { RoomId = "R1", StaffId = "S1" };
            var result = await CreateHandler(roomRepo: mockRoomRepo, accountRepo: mockAccRepo, idGen: mockId, chatRepo: mockChatRepo, notifier: mockNotifier).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            
            // Verify notification includes fallback string "Nhân viên hỗ trợ"
            mockNotifier.Verify(x => x.SendMessageToRoomAsync("R1", It.Is<ChatMessage>(m => m.Content.Contains("Nhân viên hỗ trợ"))), Times.Once);

            QACollector.LogTestCase("LiveChat - Join Support", new TestCaseDetail
            {
                FunctionGroup = "JoinSupportChat", TestCaseID = "TC-LCH-JSC-05",
                Description = "Missing active user cache forces usage of generic string 'Nhân viên hỗ trợ'",
                ExpectedResult = "Return 200, sends message parsing fallback text properly", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "staffInfo == null -> const generic text" }
            });
        }

        // TC-LCH-JSC-06 | 500 | Dependency throws
        [Fact]
        public async Task Handle_DependencyThrows_ShouldPropagateException()
        {
            var room = new ChatRoom { Members = new List<ChatRoomMember> { new() { UserId = "U1" } } };

            var mockRoomRepo = new Mock<IChatRoomRepository>();
            mockRoomRepo.Setup(x => x.GetRoomByIdAsync("R1", It.IsAny<CancellationToken>())).ReturnsAsync(room);
            var mockAccRepo = new Mock<IAccountRepository>();
            var mockId = new Mock<IIdGeneratorService>();
            var mockChatRepo = new Mock<IChatRepository>();

            var mockNotifier = new Mock<IChatNotificationService>();
            mockNotifier.Setup(x => x.SendMessageToRoomAsync(It.IsAny<string>(), It.IsAny<ChatMessage>())).ThrowsAsync(new Exception("SignalR offline"));

            var command = new JoinSupportChatCommand { RoomId = "R1", StaffId = "S1" };
            
            await Assert.ThrowsAsync<Exception>(() => CreateHandler(roomRepo: mockRoomRepo, accountRepo: mockAccRepo, idGen: mockId, chatRepo: mockChatRepo, notifier: mockNotifier).Handle(command, CancellationToken.None));

            QACollector.LogTestCase("LiveChat - Join Support", new TestCaseDetail
            {
                FunctionGroup = "JoinSupportChat", TestCaseID = "TC-LCH-JSC-06",
                Description = "Dependency throws SignalR equivalent error breaking method stack explicitly mapping exception upwards",
                ExpectedResult = "Throws natively backwards to invocation caller inside MediatR", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Unhandeled SignalR trace logic" }
            });
        }
    }
}