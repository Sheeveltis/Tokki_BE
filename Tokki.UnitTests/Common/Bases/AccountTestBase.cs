using FluentValidation;
using FluentValidation.Results;
using Moq;
using System.Threading;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.Commands.Login;
using Tokki.Application.UseCases.Accounts.Queries.Login;

namespace Tokki.UnitTests.Common.Bases
{
    public class AccountTestBase
    {
        protected readonly Mock<IAccountRepository> _mockAccountRepo;
        protected readonly Mock<ISystemConfigRepository> _mockSystemConfigRepo;
        protected readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
        protected readonly Mock<IIdGeneratorService> _mockIdGenerator;

        protected readonly Mock<IGamificationService> _mockGamificationService;
        protected readonly Mock<IValidator<LoginCommand>> _mockValidator;
        protected readonly Mock<IEmailHistoryRepository> _mockEmailHistoryRepository;

        protected readonly LoginCommandHandler _handler;

        public AccountTestBase()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockSystemConfigRepo = new Mock<ISystemConfigRepository>();
            _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
            _mockIdGenerator = new Mock<IIdGeneratorService>();

            _mockGamificationService = new Mock<IGamificationService>();
            _mockValidator = new Mock<IValidator<LoginCommand>>();
            _mockEmailHistoryRepository = new Mock<IEmailHistoryRepository>();

            // Handler hiện tại không gọi validator, nhưng vẫn mock để đúng chữ ký constructor
            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _handler = new LoginCommandHandler(
                _mockAccountRepo.Object,
                _mockSystemConfigRepo.Object,
                _mockJwtGenerator.Object,
                _mockIdGenerator.Object,
                _mockGamificationService.Object,
                _mockValidator.Object,
                _mockEmailHistoryRepository.Object
            );
        }
    }
}
