using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap.Commands
{
    public class GenerateRoadmapCommandHandlerTests
    {
        private readonly Mock<IAiRoadmapService> _mockAiRoadmapService;
        private readonly Mock<IExamAssemblyService> _mockExamAssemblyService;
        private readonly Mock<IIdGeneratorService> _mockIdGeneratorService;
        private readonly Mock<IUserRoadmapRepository> _mockUserRoadmapRepository;
        private readonly Mock<IUserWeaknessRepository> _mockUserWeaknessRepository;
        private readonly Mock<IUserExamRepository> _mockUserExamRepository;
        private readonly Mock<IAccountRepository> _mockAccountRepository;
        private readonly Mock<IRoadmapProgressService> _mockProgressService;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ILogger<GenerateRoadmapCommandHandler>> _mockLogger;

        private readonly GenerateRoadmapCommandHandler _handler;

        public GenerateRoadmapCommandHandlerTests()
        {
            _mockAiRoadmapService = new Mock<IAiRoadmapService>();
            _mockExamAssemblyService = new Mock<IExamAssemblyService>();
            _mockIdGeneratorService = new Mock<IIdGeneratorService>();
            _mockUserRoadmapRepository = new Mock<IUserRoadmapRepository>();
            _mockUserWeaknessRepository = new Mock<IUserWeaknessRepository>();
            _mockUserExamRepository = new Mock<IUserExamRepository>();
            _mockAccountRepository = new Mock<IAccountRepository>();
            _mockProgressService = new Mock<IRoadmapProgressService>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockMediator = new Mock<IMediator>();
            _mockLogger = new Mock<ILogger<GenerateRoadmapCommandHandler>>();

            _handler = new GenerateRoadmapCommandHandler(
                _mockAiRoadmapService.Object,
                _mockExamAssemblyService.Object,
                _mockIdGeneratorService.Object,
                _mockUserRoadmapRepository.Object,
                _mockUserWeaknessRepository.Object,
                _mockUserExamRepository.Object,
                _mockAccountRepository.Object,
                _mockProgressService.Object,
                _mockScopeFactory.Object,
                _mockMediator.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_ActiveRoadmapExists_ReturnsFailure400()
        {
            var command = new GenerateRoadmapCommand { UserId = "u1", DurationDays = 30, TargetAim = TargetAimLevel.Topik_II_Level5 };

            _mockUserRoadmapRepository.Setup(x => x.GetActiveRoadmapByUserIdAsync("u1", It.IsAny<CancellationToken>()))
                                      .ReturnsAsync(new UserRoadmap()); 

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Bạn đang có một lộ trình học đang hoạt động.");

            QACollector.LogTestCase("Roadmap - Generate Roadmap", new TestCaseDetail
            {
                FunctionGroup     = "GenerateRoadmapCommandHandler",
                TestCaseID        = "GenerateRoadmapCommandHandler_01",
                Description       = "Fails if an active roadmap exists",
                ExpectedResult    = "400 Failure",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Has active roadmap" }
            });
        }

        [Fact]
        public async Task Handle_NoActiveRoadmap_ReturnsSuccess202()
        {
            var command = new GenerateRoadmapCommand { UserId = "u1", DurationDays = 30, TargetAim = TargetAimLevel.Topik_II_Level5 };

            _mockUserRoadmapRepository.Setup(x => x.GetActiveRoadmapByUserIdAsync("u1", It.IsAny<CancellationToken>()))
                                      .ReturnsAsync((UserRoadmap?)null);

            var generatedJobId = "job123";
            _mockIdGeneratorService.Setup(x => x.GenerateCustom(15)).Returns(generatedJobId);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(202);
            result.Data.Should().Be(generatedJobId);

            QACollector.LogTestCase("Roadmap - Generate Roadmap", new TestCaseDetail
            {
                FunctionGroup     = "GenerateRoadmapCommandHandler",
                TestCaseID        = "GenerateRoadmapCommandHandler_02",
                Description       = "No active roadmap smoothly starts background",
                ExpectedResult    = "202 Success",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid parameters" }
            });
        }
    }
}
