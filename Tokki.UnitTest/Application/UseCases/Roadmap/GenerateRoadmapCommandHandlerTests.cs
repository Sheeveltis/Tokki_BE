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
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap
{
    public class GenerateRoadmapCommandHandlerTests
    {
        // ── Factory helpers ────────────────────────────────────────────────
        private static Mock<IIdGeneratorService> GetIdGen(string id = "JOB-001")
        {
            var m = new Mock<IIdGeneratorService>();
            m.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns(id);
            return m;
        }

        private static Mock<IRoadmapProgressService> GetProgressMock()
        {
            var m = new Mock<IRoadmapProgressService>();
            m.Setup(x => x.Set(It.IsAny<string>(), It.IsAny<RoadmapProgressState>()));
            m.Setup(x => x.Get(It.IsAny<string>())).Returns((RoadmapProgressState?)null);
            return m;
        }

        private static Mock<IServiceScopeFactory> GetScopeMock()
        {
            // The handler uses IServiceScopeFactory only inside Task.Run (background).
            // For purpose of synchronous guard tests we just need it to not throw.
            var scope       = new Mock<IServiceScope>();
            var sp          = new Mock<IServiceProvider>();
            var scopeFactory = new Mock<IServiceScopeFactory>();

            sp.Setup(x => x.GetService(It.IsAny<Type>())).Returns(null!);
            scope.Setup(x => x.ServiceProvider).Returns(sp.Object);
            scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

            return scopeFactory;
        }

        private static GenerateRoadmapCommandHandler CreateHandler(
            Mock<IUserRoadmapRepository>? roadmapRepo = null,
            Mock<IRoadmapProgressService>? progress   = null,
            Mock<IIdGeneratorService>?     idGen      = null)
        {
            var mockAi       = new Mock<IAiRoadmapService>();
            var mockExam     = new Mock<IExamAssemblyService>();
            var mockWeak     = new Mock<IUserWeaknessRepository>();
            var mockUserExam = new Mock<IUserExamRepository>();
            var mockAccount  = new Mock<IAccountRepository>();
            var mockMediator = new Mock<IMediator>();
            var mockLogger   = new Mock<ILogger<GenerateRoadmapCommandHandler>>();

            return new GenerateRoadmapCommandHandler(
                mockAi.Object,
                mockExam.Object,
                (idGen      ?? GetIdGen()).Object,
                (roadmapRepo ?? MockUserRoadmapRepository.GetMock()).Object,
                mockWeak.Object,
                mockUserExam.Object,
                mockAccount.Object,
                (progress ?? GetProgressMock()).Object,
                GetScopeMock().Object,
                mockMediator.Object,
                mockLogger.Object);
        }

        private static GenerateRoadmapCommand MakeCommand(string userId = "USER-001") => new GenerateRoadmapCommand
        {
            UserId      = userId,
            TargetAim   = TargetAimLevel.Topik_I_Level1,
            DurationDays = 14,
            UserExamId  = string.Empty
        };

        // TC-RM-GEN-01 | A | Active roadmap already exists → 400
        [Fact]
        public async Task Handle_ActiveRoadmapExists_ShouldReturn400()
        {
            var existing = MockUserRoadmapRepository.GetSampleActiveRoadmap("USER-001");
            var repo     = MockUserRoadmapRepository.GetMock(activeRoadmap: existing);
            var result   = await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Roadmap - Generate Roadmap", new TestCaseDetail
            {
                FunctionGroup = "GenerateRoadmap", TestCaseID = "TC-RM-GEN-01",
                Description = "Active roadmap already exists → 400",
                ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "GetActiveRoadmapByUserIdAsync returns non-null" }
            });
        }

        // TC-RM-GEN-02 | N | No active roadmap → returns JobId (202) immediately (fire-and-forget)
        [Fact]
        public async Task Handle_NoActiveRoadmap_ShouldReturn202WithJobId()
        {
            var repo     = MockUserRoadmapRepository.GetMock(activeRoadmap: null);
            var idGen    = GetIdGen("JOB-XYZ-001");
            var result   = await CreateHandler(repo, idGen: idGen).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(202);
            result.Data.Should().NotBeNullOrEmpty();
            QACollector.LogTestCase("Roadmap - Generate Roadmap", new TestCaseDetail
            {
                FunctionGroup = "GenerateRoadmap", TestCaseID = "TC-RM-GEN-02",
                Description = "No active roadmap → 202 Accepted with non-empty JobId",
                ExpectedResult = "IsSuccess=true, StatusCode=202, Data=JobId", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "No active roadmap", "returns JobId immediately" }
            });
        }

        // TC-RM-GEN-03 | N | JobId returned equals generated id from IIdGeneratorService
        [Fact]
        public async Task Handle_NoActiveRoadmap_JobIdMatchesGeneratedId()
        {
            var repo   = MockUserRoadmapRepository.GetMock(activeRoadmap: null);
            var idGen  = GetIdGen("FIXED-JOB-ID-123");
            var result = await CreateHandler(repo, idGen: idGen).Handle(MakeCommand(), CancellationToken.None);
            result.Data.Should().Be("FIXED-JOB-ID-123");
            QACollector.LogTestCase("Roadmap - Generate Roadmap", new TestCaseDetail
            {
                FunctionGroup = "GenerateRoadmap", TestCaseID = "TC-RM-GEN-03",
                Description = "JobId returned matches IIdGeneratorService output",
                ExpectedResult = "Data == 'FIXED-JOB-ID-123'", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "IIdGeneratorService returns 'FIXED-JOB-ID-123'" }
            });
        }

        // TC-RM-GEN-04 | B | IRoadmapProgressService.Set called with initial state (Percent=0) before returning
        [Fact]
        public async Task Handle_NoActiveRoadmap_ProgressSetCalledWithZeroPercent()
        {
            var repo     = MockUserRoadmapRepository.GetMock(activeRoadmap: null);
            var progress = GetProgressMock();
            await CreateHandler(repo, progress: progress).Handle(MakeCommand(), CancellationToken.None);

            // Set should have been called at least once synchronously with Percent=0 (initial state)
            progress.Verify(x => x.Set(
                It.IsAny<string>(),
                It.Is<RoadmapProgressState>(s => s.Percent == 0)),
                Times.AtLeastOnce);
            QACollector.LogTestCase("Roadmap - Generate Roadmap", new TestCaseDetail
            {
                FunctionGroup = "GenerateRoadmap", TestCaseID = "TC-RM-GEN-04",
                Description = "Boundary: IRoadmapProgressService.Set called with Percent=0 initially",
                ExpectedResult = "Set(_, {Percent=0}) called at least once", StatusRound1 = "Passed", TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Progress initialized synchronously before Task.Run" }
            });
        }

        // TC-RM-GEN-05 | B | GetActiveRoadmapByUserIdAsync called with correct UserId
        [Fact]
        public async Task Handle_Command_GetActiveRoadmapCalledWithCorrectUserId()
        {
            var repo = MockUserRoadmapRepository.GetMock(activeRoadmap: null);
            await CreateHandler(repo).Handle(MakeCommand("USER-SPECIFIC"), CancellationToken.None);
            repo.Verify(x => x.GetActiveRoadmapByUserIdAsync("USER-SPECIFIC", It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Generate Roadmap", new TestCaseDetail
            {
                FunctionGroup = "GenerateRoadmap", TestCaseID = "TC-RM-GEN-05",
                Description = "GetActiveRoadmapByUserIdAsync called with correct UserId",
                ExpectedResult = "Times.Once with 'USER-SPECIFIC'", StatusRound1 = "Passed", TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "UserId passed through command" }
            });
        }

        // TC-RM-GEN-06 | A | Active roadmap exists → GetActiveRoadmapByUserIdAsync called once, no progress set
        [Fact]
        public async Task Handle_ActiveRoadmapExists_NoProgressInitialized()
        {
            var existing = MockUserRoadmapRepository.GetSampleActiveRoadmap("USER-001");
            var repo     = MockUserRoadmapRepository.GetMock(activeRoadmap: existing);
            var progress = GetProgressMock();
            await CreateHandler(repo, progress: progress).Handle(MakeCommand(), CancellationToken.None);

            // If guard fails early, Set should NOT be called
            progress.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<RoadmapProgressState>()), Times.Never);
            QACollector.LogTestCase("Roadmap - Generate Roadmap", new TestCaseDetail
            {
                FunctionGroup = "GenerateRoadmap", TestCaseID = "TC-RM-GEN-06",
                Description = "Active roadmap exists → early return, IRoadmapProgressService.Set never called",
                ExpectedResult = "Times.Never for progress.Set", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Already has active roadmap → guard returns 400 before setting progress" }
            });
        }
    }
}
