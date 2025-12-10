using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate;

namespace Tokki.UnitTests.Common.Bases
{
    public class EmailTemplateTestBase
    {
        // Mock các dependencies
        protected readonly Mock<IEmailTemplateRepository> _mockRepo;
        protected readonly Mock<IIdGeneratorService> _mockIdGen;

        // Class cần test
        protected readonly CreateEmailTemplateCommandHandler _handler;

        public EmailTemplateTestBase()
        {
            _mockRepo = new Mock<IEmailTemplateRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();

            // Khởi tạo Handler với các mock object
            _handler = new CreateEmailTemplateCommandHandler(
                _mockRepo.Object,
                _mockIdGen.Object
            );
        }
    }
}