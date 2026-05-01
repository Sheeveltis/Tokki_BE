using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.SystemConfigs.Commands.Create;
using Tokki.Domain.Entities;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.SystemConfigs
{
    public class CreateSystemConfigCommandHandlerTests
    {
        private static Mock<ISystemConfigRepository> GetRepoMock(SystemConfig? existing = null)
        {
            var m = new Mock<ISystemConfigRepository>();
            m.Setup(x => x.GetByKeyAsync(It.IsAny<string>())).ReturnsAsync(existing);
            m.Setup(x => x.AddAsync(It.IsAny<SystemConfig>())).Returns(Task.CompletedTask);
            m.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            return m;
        }

        private static Mock<IValidator<CreateSystemConfigCommand>> GetValidatorMock(bool valid = true)
        {
            var m = new Mock<IValidator<CreateSystemConfigCommand>>();
            var result = valid
                ? new ValidationResult()
                : new ValidationResult(new[] { new ValidationFailure("Key", "Required") });
            m.Setup(x => x.ValidateAsync(It.IsAny<CreateSystemConfigCommand>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(result);
            return m;
        }

        private static CreateSystemConfigCommandHandler CreateHandler(
            Mock<ISystemConfigRepository>?               repo      = null,
            Mock<IValidator<CreateSystemConfigCommand>>? validator = null)
            => new CreateSystemConfigCommandHandler(
                (repo      ?? GetRepoMock()).Object,
                (validator ?? GetValidatorMock()).Object);

        private static CreateSystemConfigCommand MakeCommand(string key = "SITE_NAME")
            => new CreateSystemConfigCommand { Key = key, Value = "Tokki", Description = "App name", DataType = "string" };

        // CreateSystemConfig_01 | N | Happy path: new key → 201 Created
        [Fact]
        public async Task Handle_NewKey_ShouldReturn201WithKey()
        {
            var result = await CreateHandler().Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().Be("SITE_NAME");
            QACollector.LogTestCase("SystemConfig - Create", new TestCaseDetail { FunctionGroup = "CreateSystemConfig", TestCaseID = "CreateSystemConfig_01", Description = "New key → 201, Data=Key", ExpectedResult = "IsSuccess=true, 201, Data='SITE_NAME'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Key does not exist", "new config created" } });
        }

        // CreateSystemConfig_02 | A | Duplicate key → failure (no status code check – AppErrors.ConfigKeyDuplicated)
        [Fact]
        public async Task Handle_DuplicateKey_ShouldReturnFailure()
        {
            var existing = new SystemConfig { Key = "SITE_NAME", Value = "Old" };
            var repo     = GetRepoMock(existing: existing);
            var result   = await CreateHandler(repo).Handle(MakeCommand("SITE_NAME"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            QACollector.LogTestCase("SystemConfig - Create", new TestCaseDetail { FunctionGroup = "CreateSystemConfig", TestCaseID = "CreateSystemConfig_02", Description = "Duplicate key → failure", ExpectedResult = "IsSuccess=false (ConfigKeyDuplicated)", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetByKeyAsync returns existing config" } });
        }

        // CreateSystemConfig_03 | B | AddAsync and SaveChangesAsync both called once
        [Fact]
        public async Task Handle_NewKey_AddAndSaveBothCalledOnce()
        {
            var repo   = GetRepoMock();
            await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            repo.Verify(x => x.AddAsync(It.IsAny<SystemConfig>()), Times.Once);
            repo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("SystemConfig - Create", new TestCaseDetail { FunctionGroup = "CreateSystemConfig", TestCaseID = "CreateSystemConfig_03", Description = "AddAsync and SaveChangesAsync both called once on success", ExpectedResult = "Both Times.Once", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "New config, both persist calls verified" } });
        }

        // CreateSystemConfig_04 | N | Created config has IsActive=true
        [Fact]
        public async Task Handle_NewKey_CreatedConfigHasIsActiveTrue()
        {
            SystemConfig? captured = null;
            var repo = GetRepoMock();
            repo.Setup(x => x.AddAsync(It.IsAny<SystemConfig>()))
                .Callback<SystemConfig>(c => captured = c)
                .Returns(Task.CompletedTask);
            await CreateHandler(repo).Handle(MakeCommand(), CancellationToken.None);
            captured.Should().NotBeNull();
            captured!.IsActive.Should().BeTrue();
            QACollector.LogTestCase("SystemConfig - Create", new TestCaseDetail { FunctionGroup = "CreateSystemConfig", TestCaseID = "CreateSystemConfig_04", Description = "Created config IsActive=true by default", ExpectedResult = "SystemConfig.IsActive=true", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "IsActive defaults to true on create" } });
        }

        // CreateSystemConfig_05 | B | Duplicate key → AddAsync never called
        [Fact]
        public async Task Handle_DuplicateKey_AddAsyncNeverCalled()
        {
            var existing = new SystemConfig { Key = "DUP_KEY" };
            var repo     = GetRepoMock(existing: existing);
            await CreateHandler(repo).Handle(MakeCommand("DUP_KEY"), CancellationToken.None);
            repo.Verify(x => x.AddAsync(It.IsAny<SystemConfig>()), Times.Never);
            QACollector.LogTestCase("SystemConfig - Create", new TestCaseDetail { FunctionGroup = "CreateSystemConfig", TestCaseID = "CreateSystemConfig_05", Description = "Duplicate key → early return, AddAsync never called", ExpectedResult = "AddAsync Times.Never", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Guard returns before AddAsync" } });
        }

        // CreateSystemConfig_06 | N | Config key in result matches request key
        [Fact]
        public async Task Handle_NewKey_ResultDataEqualsRequestKey()
        {
            var result = await CreateHandler().Handle(MakeCommand("CUSTOM_KEY"), CancellationToken.None);
            result.Data.Should().Be("CUSTOM_KEY");
            QACollector.LogTestCase("SystemConfig - Create", new TestCaseDetail { FunctionGroup = "CreateSystemConfig", TestCaseID = "CreateSystemConfig_06", Description = "Result.Data='CUSTOM_KEY' matches request Key", ExpectedResult = "Data='CUSTOM_KEY'", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Key echoed back in response" } });
        }
    }
}
