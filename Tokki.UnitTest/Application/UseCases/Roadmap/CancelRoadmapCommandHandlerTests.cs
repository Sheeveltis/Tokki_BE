using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Roadmap.Commands.CancelRoadmap;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap
{
    public class CancelRoadmapCommandHandlerTests
    {
        private static CancelRoadmapCommandHandler CreateHandler(Mock<IUserRoadmapRepository>? repo = null)
            => new CancelRoadmapCommandHandler((repo ?? MockUserRoadmapRepository.GetMock()).Object);

        // TC-RM-CAN-01 | A | No active roadmap → 404
        [Fact]
        public async Task Handle_NoActiveRoadmap_ShouldReturn404()
        {
            var repo   = MockUserRoadmapRepository.GetMock(activeRoadmap: null);
            var result = await CreateHandler(repo).Handle(new CancelRoadmapCommand { UserId = "USER-001" }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Cancel", new TestCaseDetail { FunctionGroup = "CancelRoadmap", TestCaseID = "TC-RM-CAN-01", Description = "No active roadmap → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetActiveRoadmapByUserIdAsync returns null" } });
        }

        // TC-RM-CAN-02 | N | Active roadmap exists → status set to Dropped, success
        [Fact]
        public async Task Handle_ActiveRoadmap_ShouldSetDroppedAndReturn200()
        {
            var roadmap = MockUserRoadmapRepository.GetSampleActiveRoadmap();
            var repo    = MockUserRoadmapRepository.GetMock(activeRoadmap: roadmap);
            var result  = await CreateHandler(repo).Handle(new CancelRoadmapCommand { UserId = roadmap.UserId }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            roadmap.CurrentStatus.Should().Be(UserRoadmapStatus.Dropped);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Cancel", new TestCaseDetail { FunctionGroup = "CancelRoadmap", TestCaseID = "TC-RM-CAN-02", Description = "Active roadmap → Dropped, 200", ExpectedResult = "IsSuccess=true, Status=Dropped", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Active roadmap found", "Status set to Dropped" } });
        }

        // TC-RM-CAN-03 | B | SaveChangesAsync called once after status change
        [Fact]
        public async Task Handle_ActiveRoadmap_SaveChangesCalledOnce()
        {
            var roadmap = MockUserRoadmapRepository.GetSampleActiveRoadmap();
            var repo    = MockUserRoadmapRepository.GetMock(activeRoadmap: roadmap);
            await CreateHandler(repo).Handle(new CancelRoadmapCommand { UserId = roadmap.UserId }, CancellationToken.None);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Cancel", new TestCaseDetail { FunctionGroup = "CancelRoadmap", TestCaseID = "TC-RM-CAN-03", Description = "Boundary: SaveChangesAsync called exactly once", ExpectedResult = "SaveChangesAsync Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "SaveChangesAsync called once" } });
        }

        // TC-RM-CAN-04 | A | Repository throws → exception propagates
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var roadmap = MockUserRoadmapRepository.GetSampleActiveRoadmap();
            var repo    = MockUserRoadmapRepository.GetMock(activeRoadmap: roadmap);
            repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));
            var act = async () => await CreateHandler(repo).Handle(new CancelRoadmapCommand { UserId = "USER-001" }, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
            QACollector.LogTestCase("Roadmap - Cancel", new TestCaseDetail { FunctionGroup = "CancelRoadmap", TestCaseID = "TC-RM-CAN-04", Description = "SaveChangesAsync throws → exception propagates", ExpectedResult = "Exception thrown", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "SaveChangesAsync throws" } });
        }

        // TC-RM-CAN-05 | N | UserId is passed correctly to GetActiveRoadmapByUserIdAsync
        [Fact]
        public async Task Handle_ValidUser_GetActiveRoadmapCalledWithCorrectUserId()
        {
            var repo = MockUserRoadmapRepository.GetMock(activeRoadmap: null);
            await CreateHandler(repo).Handle(new CancelRoadmapCommand { UserId = "USER-XYZ" }, CancellationToken.None);
            repo.Verify(x => x.GetActiveRoadmapByUserIdAsync("USER-XYZ", It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Cancel", new TestCaseDetail { FunctionGroup = "CancelRoadmap", TestCaseID = "TC-RM-CAN-05", Description = "GetActiveRoadmapByUserIdAsync called with correct UserId", ExpectedResult = "GetActiveRoadmapByUserIdAsync('USER-XYZ') Once", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "UserId passed correctly" } });
        }

        // TC-RM-CAN-06 | N | Status is specifically Dropped (not just changed)
        [Fact]
        public async Task Handle_CancelRoadmap_StatusIsExactlyDropped()
        {
            var roadmap = MockUserRoadmapRepository.GetSampleActiveRoadmap();
            var repo    = MockUserRoadmapRepository.GetMock(activeRoadmap: roadmap);
            await CreateHandler(repo).Handle(new CancelRoadmapCommand { UserId = roadmap.UserId }, CancellationToken.None);
            roadmap.CurrentStatus.Should().Be(UserRoadmapStatus.Dropped);
            QACollector.LogTestCase("Roadmap - Cancel", new TestCaseDetail { FunctionGroup = "CancelRoadmap", TestCaseID = "TC-RM-CAN-06", Description = "Cancelled roadmap status is exactly UserRoadmapStatus.Dropped", ExpectedResult = "CurrentStatus=Dropped", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "UserRoadmapStatus.Dropped applied" } });
        }
    }
}
