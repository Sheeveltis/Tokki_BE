using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.LiveChat.Commands.CreateSupportChat;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Mocks.Services;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.LiveChat
{
    public class CreateSupportChatCommandHandlerTests
    {
        private CreateSupportChatCommandHandler CreateHandler(
            Mock<IChatRoomRepository>? chatRoomRepo = null)
        {
            return new CreateSupportChatCommandHandler(
                (chatRoomRepo ?? new Mock<IChatRoomRepository>()).Object,
                MockIdGeneratorService.GetMock().Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateRoomAndMemberAndReturnRoomId()
        {
            var command = new CreateSupportChatCommand { UserId = "USER-001" };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.AddRoomAsync(It.IsAny<ChatRoom>()))
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.AddMemberAsync(It.IsAny<ChatRoomMember>()))
                    .Returns(Task.CompletedTask);
            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

            var handler = CreateHandler(chatRoomRepo: mockRepo); var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();

            mockRepo.Verify(x => x.AddRoomAsync(It.IsAny<ChatRoom>()), Times.Once);
            mockRepo.Verify(x => x.AddMemberAsync(It.IsAny<ChatRoomMember>()), Times.Once);

            QACollector.LogTestCase("LiveChat - Create Support Chat", new TestCaseDetail
            {
                FunctionGroup = "Create Support Chat",
                TestCaseID = "TC-CHAT-CRE-01",
                Description = "Tạo support chat hợp lệ → tạo room + member, trả về ChatRoomId",
                ExpectedResult = "Return Success, AddRoomAsync + AddMemberAsync called once",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid UserId",
                    "Room + Member created",
                    "Return ChatRoomId"
                }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var command = new CreateSupportChatCommand { UserId = "USER-001" };

            var mockRepo = new Mock<IChatRoomRepository>();
            mockRepo.Setup(x => x.AddRoomAsync(It.IsAny<ChatRoom>()))
                    .ThrowsAsync(new Exception("DB error"));

            var handler = CreateHandler(chatRoomRepo: mockRepo);
            var act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                     .WithMessage("DB error");

            QACollector.LogTestCase("LiveChat - Create Support Chat", new TestCaseDetail
            {
                FunctionGroup = "Create Support Chat",
                TestCaseID = "TC-CHAT-CRE-02",
                Description = "Repository throw exception → propagate lên caller",
                ExpectedResult = "Exception propagates",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "AddRoomAsync throws Exception",
                    "No try/catch in handler",
                    "Exception propagates"
                }
            });
        }
    }
}