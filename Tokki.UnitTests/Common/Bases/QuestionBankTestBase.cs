using Microsoft.Extensions.Logging;
using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.ActivateQuestionBanks;

namespace Tokki.UnitTests.Common.Bases
{
    public abstract class QuestionBankTestBase
    {
        protected readonly Mock<IQuestionBankRepository> _mockQuestionBankRepo;
        protected readonly Mock<IQuestionOptionRepository> _mockQuestionOptionRepo;
        protected readonly Mock<IQuestionTypeRepository> _mockQuestionTypeRepo;
        protected readonly Mock<IPassageRepository> _mockPassageRepo;
        protected readonly Mock<IIdGeneratorService> _mockIdGenerator;
        protected readonly Mock<ILogger<ActivateQuestionBanksCommandHandler>> _mockActivateLogger;

        protected QuestionBankTestBase()
        {
            _mockQuestionBankRepo = new Mock<IQuestionBankRepository>(MockBehavior.Loose);
            _mockQuestionOptionRepo = new Mock<IQuestionOptionRepository>(MockBehavior.Loose);
            _mockQuestionTypeRepo = new Mock<IQuestionTypeRepository>(MockBehavior.Loose);
            _mockPassageRepo = new Mock<IPassageRepository>(MockBehavior.Loose);
            _mockIdGenerator = new Mock<IIdGeneratorService>(MockBehavior.Loose);
            _mockActivateLogger = new Mock<ILogger<ActivateQuestionBanksCommandHandler>>(MockBehavior.Loose);
        }
    }
}
