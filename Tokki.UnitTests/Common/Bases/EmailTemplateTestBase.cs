using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate;

namespace Tokki.UnitTests.Common.Bases
{
    public class EmailTemplateTestBase
    {
        protected readonly Mock<IEmailTemplateRepository> _mockRepo;
        protected readonly Mock<IIdGeneratorService> _mockIdGen;

        protected readonly CreateEmailAutoTemplateCommandHandler _handler;

        public EmailTemplateTestBase()
        {
            _mockRepo = new Mock<IEmailTemplateRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();

            _handler = new CreateEmailAutoTemplateCommandHandler(
                _mockRepo.Object,
                _mockIdGen.Object
            );
        }
    }
}
