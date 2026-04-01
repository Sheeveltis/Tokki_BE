using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.SystemConfigs.Commands.Update;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.SystemConfigs
{
    public class UpdateSystemConfigCommandHandlerTests
    {
        private static Mock<ISystemConfigRepository> GetRepoMock(SystemConfig? config = null)
        {
            var m = new Mock<ISystemConfigRepository>();
            m.Setup(x => x.GetByKeyAsync(It.IsAny<string>())).ReturnsAsync(config);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            return m;
        }

        private static UpdateSystemConfigCommandHandler CreateHandler(Mock<ISystemConfigRepository>? repo = null)
            => new UpdateSystemConfigCommandHandler((repo ?? GetRepoMock()).Object);

        private static UpdateSystemConfigCommand MakeCommand(string key = "SITE_NAME", bool isActive = true)
            => new UpdateSystemConfigCommand { Key = key, Value = "New Value", Description = "Updated desc", IsActive = isActive };

        private static SystemConfig SampleConfig(string key = "SITE_NAME") =>
            new SystemConfig { Key = key, Value = "Old Value", Description = "Old desc", IsActive = false };

        // TC-SYS-UPD-01 | N | Happy path: config found → updated and 200
        [Fact]
        public async Task Handle_ConfigFound_ShouldReturn200WithKey()
        {
            var config = SampleConfig();
            var repo   = GetRepoMock(config);
            var result = await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().Be("SITE_NAME");
            QACollector.LogTestCase("SystemConfig - Update", new TestCaseDetail { FunctionGroup = "UpdateSystemConfig", TestCaseID = "TC-SYS-UPD-01", Description = "Config found → 200, Data=Key", ExpectedResult = "IsSuccess=true, 200, Data='SITE_NAME'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Config exists", "updated successfully" } });
        }

        // TC-SYS-UPD-02 | A | Config not found → failure
        [Fact]
        public async Task Handle_ConfigNotFound_ShouldReturnFailure()
        {
            var repo   = GetRepoMock(config: null);
            var result = await CreateHandler(repo).Handle(MakeCommand("MISSING_KEY"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("SystemConfig - Update", new TestCaseDetail { FunctionGroup = "UpdateSystemConfig", TestCaseID = "TC-SYS-UPD-02", Description = "Config not found → failure (ConfigNotFound)", ExpectedResult = "IsSuccess=false", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByKeyAsync returns null" } });
        }

        // TC-SYS-UPD-03 | N | Value and Description updated correctly
        [Fact]
        public async Task Handle_ConfigFound_ValueAndDescriptionUpdated()
        {
            var config = SampleConfig();
            var repo   = GetRepoMock(config);
            await CreateHandler(repo).Handle(new UpdateSystemConfigCommand { Key = "SITE_NAME", Value = "Tokki App", Description = "New desc", IsActive = true }, CancellationToken.None);
            config.Value.Should().Be("Tokki App");
            config.Description.Should().Be("New desc");
            QACollector.LogTestCase("SystemConfig - Update", new TestCaseDetail { FunctionGroup = "UpdateSystemConfig", TestCaseID = "TC-SYS-UPD-03", Description = "Value='Tokki App' and Description='New desc' updated", ExpectedResult = "Config.Value and Description updated", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Value and Description mutated on entity" } });
        }

        // TC-SYS-UPD-04 | N | IsActive toggled correctly
        [Fact]
        public async Task Handle_ConfigFound_IsActiveUpdatedToFalse()
        {
            var config = SampleConfig(); // originally IsActive=false
            var repo   = GetRepoMock(config);
            await CreateHandler(repo).Handle(MakeCommand(isActive: false), CancellationToken.None);
            config.IsActive.Should().BeFalse();
            QACollector.LogTestCase("SystemConfig - Update", new TestCaseDetail { FunctionGroup = "UpdateSystemConfig", TestCaseID = "TC-SYS-UPD-04", Description = "IsActive toggled to false", ExpectedResult = "Config.IsActive=false", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "IsActive=false in request" } });
        }

        // TC-SYS-UPD-05 | B | SaveChangesAsync called once on success
        [Fact]
        public async Task Handle_ConfigFound_SaveChangesCalledOnce()
        {
            var repo = GetRepoMock(SampleConfig());
            await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("SystemConfig - Update", new TestCaseDetail { FunctionGroup = "UpdateSystemConfig", TestCaseID = "TC-SYS-UPD-05", Description = "SaveChangesAsync called once on successful update", ExpectedResult = "Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Save called after mutation" } });
        }

        // TC-SYS-UPD-06 | B | Config not found → SaveChangesAsync never called
        [Fact]
        public async Task Handle_ConfigNotFound_SaveChangesNeverCalled()
        {
            var repo = GetRepoMock(config: null);
            await CreateHandler(repo).Handle(MakeCommand("MISSING"), CancellationToken.None);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            QACollector.LogTestCase("SystemConfig - Update", new TestCaseDetail { FunctionGroup = "UpdateSystemConfig", TestCaseID = "TC-SYS-UPD-06", Description = "Config not found → early return, SaveChangesAsync never called", ExpectedResult = "Times.Never", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Guard returns before SaveChanges" } });
        }
    }
}
