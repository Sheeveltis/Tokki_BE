using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.VipPackages.Commands.CreateVipPackage;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.VipPackages
{
    public class CreateVipPackageHandlerTests
    {
        // Tạo mock IdGenerator riêng trong file test này
        // để không phụ thuộc vào MockIdGeneratorService cũ
        private static Mock<IIdGeneratorService> CreateIdGenMock()
        {
            var mock = new Mock<IIdGeneratorService>();
            mock.Setup(x => x.Generate()).Returns("TK_FAKE_ID_999");
            mock.Setup(x => x.Generate(It.IsAny<int>())).Returns("TK_FAKE_ID_999");
            mock.Setup(x => x.GenerateCustom(It.IsAny<int>())).Returns("TK_FAKE_ID_999");
            return mock;
        }

        private CreateVipPackageHandler CreateHandler(
            Mock<IVipPackageRepository>? repo = null)
        {
            return new CreateVipPackageHandler(
                (repo ?? MockVipPackageRepository.GetMock()).Object,
                CreateIdGenMock().Object); // dùng mock local thay vì MockIdGeneratorService
        }

        [Fact]
        public async Task Handle_NegativePrice_ShouldReturnFailure()
        {
            var command = new CreateVipPackageCommand
            {
                Name = "VIP Test",
                Price = -1,
                DurationDays = 30
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("VipPackage - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Vip Package",
                TestCaseID = "TC-VIP-CRE-01",
                Description = "Create VIP package with negative price (-1)",
                ExpectedResult = "Return Failure VipPackageInvalidPrice",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Price = -1 (invalid)",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_ZeroOrNegativeDurationDays_ShouldReturnFailure()
        {
            var command = new CreateVipPackageCommand
            {
                Name = "VIP Test",
                Price = 99000,
                DurationDays = 0
            };

            var handler = CreateHandler();
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("VipPackage - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Vip Package",
                TestCaseID = "TC-VIP-CRE-02",
                Description = "Create a VIP package with DurationDays = 0 (boundary: invalid minimum)",
                ExpectedResult = "Return Failure VipPackageInvalidDuration",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "DurationDays = 0 (boundary: <= 0)",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidData_ShouldCreateInactivePackageAndReturnId()
        {
            // Arrange
            var command = new CreateVipPackageCommand
            {
                Name = "VIP Basic",
                PackageType = "Monthly",
                Price = 99000,
                DurationDays = 30,
                Description = "Basic package"
            };

            // Capture package được AddAsync nhận vào
            Tokki.Domain.Entities.VipPackage? capturedPackage = null;

            var mockRepo = MockVipPackageRepository.GetMock();
            mockRepo.Setup(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.VipPackage>()))
                    .Callback<Tokki.Domain.Entities.VipPackage>(p => capturedPackage = p)
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(repo: mockRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();

            capturedPackage.Should().NotBeNull();
            capturedPackage!.IsActive.Should().BeFalse();
            capturedPackage.Price.Should().Be(99000);
            capturedPackage.DurationDays.Should().Be(30);
            capturedPackage.Name.Should().Be("VIP Basic");

            mockRepo.Verify(x => x.AddAsync(
                It.IsAny<Tokki.Domain.Entities.VipPackage>()),
                Times.Once);

            QACollector.LogTestCase("VipPackage - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Vip Package",
                TestCaseID = "TC-VIP-CRE-03",
                Description = "Create a valid VIP package → IsActive = false (not activated), return PackageId",
                ExpectedResult = "Return Success, Data = PackageId, IsActive = false",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid Price and DurationDays",
                    "IsActive = false (default not active)",
                    "AddAsync called once",
                    "Return Success"
                }
            });
        }

        // ── TC-04: AddAsync ném exception → catch trả về failure ─────────
        [Fact]
        public async Task Handle_AddAsyncThrows_ShouldReturnFailure()
        {
            // Arrange
            var command = new CreateVipPackageCommand
            {
                Name = "Error Plan",
                Price = 99000,
                DurationDays = 30
            };

            var mockRepo = new Mock<IVipPackageRepository>();
            mockRepo.Setup(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.VipPackage>()))
                    .ThrowsAsync(new Exception("DB write error"));

            var handler = CreateHandler(repo: mockRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("VipPackage - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Vip Package",
                TestCaseID = "TC-VIP-CRE-04",
                Description = "AddAsync throws exception → catch block returns failure",
                ExpectedResult = "IsSuccess=false (VipPackageCreationFailed)",
                StatusRound1 = "Passed",
                TestCaseType = "E",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "AddAsync throws Exception",
                    "Catch block returns failure"
                }
            });
        }

        // ── TC-05: Price = 0 là hợp lệ (boundary: Price không âm) ────────
        [Fact]
        public async Task Handle_ZeroPrice_ShouldBeValidAndCreatePackage()
        {
            // Arrange
            var command = new CreateVipPackageCommand
            {
                Name = "Free Trial",
                Price = 0,
                DurationDays = 3
            };

            var mockRepo = MockVipPackageRepository.GetMock();
            mockRepo.Setup(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.VipPackage>()))
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(repo: mockRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockRepo.Verify(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.VipPackage>()), Times.Once);

            QACollector.LogTestCase("VipPackage - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Vip Package",
                TestCaseID = "TC-VIP-CRE-05",
                Description = "Price = 0 is valid (boundary: condition is Price < 0)",
                ExpectedResult = "IsSuccess=true, AddAsync called once",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Price = 0 (boundary, not < 0)",
                    "DurationDays = 3 (valid)",
                    "Return success"
                }
            });
        }

        // ── TC-06: UpdateAsync không bao giờ được gọi khi tạo mới ────────
        [Fact]
        public async Task Handle_ValidCreate_ShouldNeverCallUpdateAsync()
        {
            // Arrange
            var command = new CreateVipPackageCommand
            {
                Name = "Standard Plan",
                Price = 149000,
                DurationDays = 30
            };

            var mockRepo = MockVipPackageRepository.GetMock();
            mockRepo.Setup(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.VipPackage>()))
                    .Returns(Task.CompletedTask);

            var handler = CreateHandler(repo: mockRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockRepo.Verify(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.VipPackage>()), Times.Never);

            QACollector.LogTestCase("VipPackage - Create", new TestCaseDetail
            {
                FunctionGroup = "Create Vip Package",
                TestCaseID = "TC-VIP-CRE-06",
                Description = "Create flow should never call UpdateAsync",
                ExpectedResult = "IsSuccess=true, UpdateAsync never called",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid command",
                    "UpdateAsync Times.Never",
                    "Only AddAsync is called"
                }
            });
        }
    }
}