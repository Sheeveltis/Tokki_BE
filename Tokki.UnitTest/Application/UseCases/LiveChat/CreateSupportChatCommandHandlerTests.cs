using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.LiveChat.Commands.CreateSupportChat;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.LiveChat
{
    public class CreateSupportChatCommandHandlerTests
    {
        private static CreateSupportChatCommandHandler CreateHandler(
            Mock<IChatRoomRepository>? chatRoomRepo = null,
            Mock<IIdGeneratorService>? idGen = null)
        {
            return new CreateSupportChatCommandHandler(
                (chatRoomRepo ?? new Mock<IChatRoomRepository>()).Object,
                (idGen ?? new Mock<IIdGeneratorService>()).Object);
        }

        // CreateSupportChat_01 | N | Create Room valid logic verification
        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateRoomProperly()
        {
            var mockRepo = new Mock<IChatRoomRepository>();
            var mockId = new Mock<IIdGeneratorService>();
            mockId.Setup(x => x.Generate(10)).Returns("ROOM1");
            mockId.Setup(x => x.Generate(15)).Returns("MEM1");

            var command = new CreateSupportChatCommand { UserId = "USER1" };
            await CreateHandler(mockRepo, mockId).Handle(command, CancellationToken.None);

            mockRepo.Verify(x => x.AddRoomAsync(It.Is<ChatRoom>(r => 
                r.ChatRoomId == "ROOM1" && 
                r.IsSupport == true && 
                r.IsClosed == false && 
                r.IsGroup == true && 
                r.Name == "Hỗ trợ tư vấn")), Times.Once);

            QACollector.LogTestCase("LiveChat - Create Support", new TestCaseDetail
            {
                FunctionGroup = "CreateSupportChat", TestCaseID = "CreateSupportChat_01",
                Description = "Ensures correctly structured chat entity builds properly in memory",
                ExpectedResult = "Return 200, AddRoom invoked comprehensively configured", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ChatRoom mapping specifics validation" }
            });
        }

        // CreateSupportChat_02 | N | Create Admin member with correct binding
        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateAdminMember()
        {
            var mockRepo = new Mock<IChatRoomRepository>();
            var mockId = new Mock<IIdGeneratorService>();
            mockId.Setup(x => x.Generate(10)).Returns("ROOM1");
            mockId.Setup(x => x.Generate(15)).Returns("MEM1");

            var command = new CreateSupportChatCommand { UserId = "USER1" };
            await CreateHandler(mockRepo, mockId).Handle(command, CancellationToken.None);

            mockRepo.Verify(x => x.AddMemberAsync(It.Is<ChatRoomMember>(m => 
                m.ChatRoomMemberId == "MEM1" && 
                m.ChatRoomId == "ROOM1" && 
                m.UserId == "USER1" && 
                m.IsAdmin == true)), Times.Once);

            QACollector.LogTestCase("LiveChat - Create Support", new TestCaseDetail
            {
                FunctionGroup = "CreateSupportChat", TestCaseID = "CreateSupportChat_02",
                Description = "Ensures correct member permissions configured locally during first support execution",
                ExpectedResult = "Return 200, AddMember invoked validating member", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Member IsAdmin=true bindings checked" }
            });
        }

        // CreateSupportChat_03 | N | Check Output Success String
        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnProperRoomId()
        {
            var mockRepo = new Mock<IChatRoomRepository>();
            var mockId = new Mock<IIdGeneratorService>();
            mockId.Setup(x => x.Generate(10)).Returns("GEN_ROOM");
            
            var command = new CreateSupportChatCommand { UserId = "U1" };
            var result = await CreateHandler(mockRepo, mockId).Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("GEN_ROOM");

            QACollector.LogTestCase("LiveChat - Create Support", new TestCaseDetail
            {
                FunctionGroup = "CreateSupportChat", TestCaseID = "CreateSupportChat_03",
                Description = "Expected RoomID resolves correctly passed back",
                ExpectedResult = "Returns RoomId in OperationResult", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "result.Data corresponds dynamically" }
            });
        }

        // CreateSupportChat_04 | A | AddRoomThrows
        [Fact]
        public async Task Handle_AddRoomThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.AddRoomAsync(It.IsAny<ChatRoom>())).ThrowsAsync(new Exception("EF Validation Error"));
            var mockId = new Mock<IIdGeneratorService>();

            var command = new CreateSupportChatCommand { UserId = "U1" };
            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo, mockId).Handle(command, CancellationToken.None));

            QACollector.LogTestCase("LiveChat - Create Support", new TestCaseDetail
            {
                FunctionGroup = "CreateSupportChat", TestCaseID = "CreateSupportChat_04",
                Description = "Repository failure during Room addition breaks early",
                ExpectedResult = "Throws Exception internally mapped", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Unhandled EF AddRoom Exception block" }
            });
        }

        // CreateSupportChat_05 | A | AddMemberThrows
        [Fact]
        public async Task Handle_AddMemberThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.AddMemberAsync(It.IsAny<ChatRoomMember>())).ThrowsAsync(new Exception("Foreign Key Missing"));
            var mockId = new Mock<IIdGeneratorService>();

            var command = new CreateSupportChatCommand { UserId = "U1" };
            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo, mockId).Handle(command, CancellationToken.None));

            QACollector.LogTestCase("LiveChat - Create Support", new TestCaseDetail
            {
                FunctionGroup = "CreateSupportChat", TestCaseID = "CreateSupportChat_05",
                Description = "Repository failure during linking breaks creation sequence",
                ExpectedResult = "Throws native Exception tracking downwards", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Unhandled exception thrown within execution" }
            });
        }

        // CreateSupportChat_06 | A | SaveChangesThrows
        [Fact]
        public async Task Handle_SaveChangesThrows_ShouldPropagateException()
        {
            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Commit Issue"));
            var mockId = new Mock<IIdGeneratorService>();

            var command = new CreateSupportChatCommand { UserId = "U1" };
            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mockRepo, mockId).Handle(command, CancellationToken.None));

            QACollector.LogTestCase("LiveChat - Create Support", new TestCaseDetail
            {
                FunctionGroup = "CreateSupportChat", TestCaseID = "CreateSupportChat_06",
                Description = "Global sync commit propagates internal trace outwards correctly",
                ExpectedResult = "Throws natively tracking errors backwards", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Unhandeled database failure" }
            });
        }
    }
}