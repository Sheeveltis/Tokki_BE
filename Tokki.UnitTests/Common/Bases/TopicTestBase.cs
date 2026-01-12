using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;

namespace Tokki.UnitTests.Common.Bases
{
    public abstract class TopicTestBase
    {
        protected const string DefaultUserId = "user-test-01";

        protected readonly Mock<ITopicRepository> _mockTopicRepo;
        protected readonly Mock<IVocabularyRepository> _mockVocabRepo;
        protected readonly Mock<IVocabularyTopicRepository> _mockVocabTopicRepo;

        protected readonly Mock<IAccountRepository> _mockAccountRepo;
        protected readonly Mock<IEmailService> _mockEmailService;
        protected readonly Mock<IIdGeneratorService> _mockIdGen;

        protected readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

        protected readonly Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.CreateTopic.CreateTopicCommandHandler>> _mockCreateTopicLogger;
        protected readonly Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.CreateTopicByStaff.CreateTopicByStaffCommandHandler>> _mockCreateTopicByStaffLogger;
        protected readonly Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.DeleteTopic.DeleteTopicCommandHandler>> _mockDeleteTopicLogger;
        protected readonly Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.UpdateTopic.UpdateTopicCommandHandler>> _mockUpdateTopicLogger;
        protected readonly Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.ApproveTopic.ApproveTopicCommandHandler>> _mockApproveLogger;
        protected readonly Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.RejectTopic.RejectTopicCommandHandler>> _mockRejectLogger;
        protected readonly Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.SubmitTopicForApproval.SubmitTopicForApprovalCommandHandler>> _mockSubmitLogger;

        protected TopicTestBase()
        {
            _mockTopicRepo = new Mock<ITopicRepository>(MockBehavior.Loose);
            _mockVocabRepo = new Mock<IVocabularyRepository>(MockBehavior.Loose);
            _mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>(MockBehavior.Loose);

            _mockAccountRepo = new Mock<IAccountRepository>(MockBehavior.Loose);
            _mockEmailService = new Mock<IEmailService>(MockBehavior.Loose);
            _mockIdGen = new Mock<IIdGeneratorService>(MockBehavior.Loose);

            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Loose);

            _mockCreateTopicLogger = new Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.CreateTopic.CreateTopicCommandHandler>>();
            _mockCreateTopicByStaffLogger = new Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.CreateTopicByStaff.CreateTopicByStaffCommandHandler>>();
            _mockDeleteTopicLogger = new Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.DeleteTopic.DeleteTopicCommandHandler>>();
            _mockUpdateTopicLogger = new Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.UpdateTopic.UpdateTopicCommandHandler>>();
            _mockApproveLogger = new Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.ApproveTopic.ApproveTopicCommandHandler>>();
            _mockRejectLogger = new Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.RejectTopic.RejectTopicCommandHandler>>();
            _mockSubmitLogger = new Mock<ILogger<Tokki.Application.UseCases.Topics.Commands.SubmitTopicForApproval.SubmitTopicForApprovalCommandHandler>>();

            SetupAuthenticatedUser(DefaultUserId);
        }

        protected void SetupAuthenticatedUser(string userId)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var context = new DefaultHttpContext { User = principal };

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);
        }

        protected void SetupUnauthenticatedUser()
        {
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        }
    }
}
