using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;

namespace Tokki.UnitTests.Common.Bases
{
    public abstract class QuestionOptionTestBase
    {
        protected readonly Mock<IQuestionBankRepository> _mockQuestionBankRepo;
        protected readonly Mock<IQuestionOptionRepository> _mockQuestionOptionRepo;
        protected readonly Mock<IQuestionTypeRepository> _mockQuestionTypeRepo;
        protected readonly Mock<IIdGeneratorService> _mockIdGenerator;

        protected QuestionOptionTestBase()
        {
            _mockQuestionBankRepo = new Mock<IQuestionBankRepository>(MockBehavior.Loose);
            _mockQuestionOptionRepo = new Mock<IQuestionOptionRepository>(MockBehavior.Loose);
            _mockQuestionTypeRepo = new Mock<IQuestionTypeRepository>(MockBehavior.Loose);
            _mockIdGenerator = new Mock<IIdGeneratorService>(MockBehavior.Loose);
        }
    }
}
