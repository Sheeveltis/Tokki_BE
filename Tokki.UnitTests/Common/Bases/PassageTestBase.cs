using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;

namespace Tokki.UnitTests.Common.Bases
{
    public abstract class PassageTestBase
    {
        protected readonly Mock<IPassageRepository> _mockPassageRepo;
        protected readonly Mock<IQuestionBankRepository> _mockQuestionBankRepo;
        protected readonly Mock<IIdGeneratorService> _mockIdGenerator;

        protected PassageTestBase()
        {
            _mockPassageRepo = new Mock<IPassageRepository>(MockBehavior.Loose);
            _mockQuestionBankRepo = new Mock<IQuestionBankRepository>(MockBehavior.Loose);
            _mockIdGenerator = new Mock<IIdGeneratorService>(MockBehavior.Loose);
        }
    }
}
