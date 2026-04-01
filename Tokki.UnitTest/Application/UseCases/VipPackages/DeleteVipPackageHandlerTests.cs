using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VipPackages.Commands.DeleteVipPackage;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.VipPackages
{
    public class DeleteVipPackageHandlerTests
    {
        private DeleteVipPackageHandler CreateHandler(
            Mock<IVipPackageRepository>? repo = null)
        {
            return new DeleteVipPackageHandler(
                (repo ?? MockVipPackageRepository.GetMock()).Object);
        }

        [Fact]
        public async Task Handle_PackageNotFound_ShouldReturnFailure()
        {
            var command = new DeleteVipPackageCommand { Id = "PKG-INVALID" };

            var handler = CreateHandler(
                repo: MockVipPackageRepository.GetMock(returnedPackage: null));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("VipPackage - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vip Package",
                TestCaseID = "TC-VIP-DEL-01",
                Description = "Delete VIP package with non-existing ID",
                ExpectedResult = "Return Failure VipPackageNotFound",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Invalid Package Id",
                    "Package = null",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidPackage_ShouldSetIsActiveFalseAndReturnSuccess()
        {
            var existingPackage = MockVipPackageRepository.GetSamplePackage();
            existingPackage.IsActive = true; // đang active

            var mockRepo = MockVipPackageRepository.GetMock(
                returnedPackage: existingPackage);

            var command = new DeleteVipPackageCommand { Id = "PKG-001" };
            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            // Soft delete: IsActive = false
            existingPackage.IsActive.Should().BeFalse();

            mockRepo.Verify(
                x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.VipPackage>()),
                Times.Once);

            QACollector.LogTestCase("VipPackage - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vip Package",
                TestCaseID = "TC-VIP-DEL-02",
                Description = "Delete a valid VIP Active package → IsActive = false (soft delete), return Success",
                ExpectedResult = "Return Success, IsActive = false, UpdateAsync called once",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid Package Id",
                    "IsActive = true → false (soft delete)",
                    "UpdateAsync called once",
                    "Return Success"
                }
            });
        }

        [Fact]
        public async Task Handle_AlreadyInactivePackage_ShouldStillSetIsActiveFalseAndReturnSuccess()
        {
            // Gói đã inactive → vẫn gọi Update và return Success (idempotent)
            var inactivePackage = MockVipPackageRepository.GetSampleInactivePackage();

            var mockRepo = MockVipPackageRepository.GetMock(
                returnedPackage: inactivePackage);

            var command = new DeleteVipPackageCommand { Id = "PKG-002" };
            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            inactivePackage.IsActive.Should().BeFalse();

            QACollector.LogTestCase("VipPackage - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vip Package",
                TestCaseID = "TC-VIP-DEL-03",
                Description = "Delete the VIP package with IsActive = false → idempotent, still return Success",
                ExpectedResult = "Return Success, IsActive hold = false",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "IsActive = false (boundary: already inactive)",
                    "Idempotent → return Success"
                }
            });
        }

        // ── TC-04: GetByIdAsync được gọi đúng Id ────────────────────────
        [Fact]
        public async Task Handle_ValidId_ShouldCallGetByIdAsyncWithCorrectId()
        {
            // Arrange
            var package = MockVipPackageRepository.GetSamplePackage("PKG-XYZ");
            var mockRepo = MockVipPackageRepository.GetMock(returnedPackage: package);

            var command = new DeleteVipPackageCommand { Id = "PKG-XYZ" };
            var handler = CreateHandler(repo: mockRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockRepo.Verify(x => x.GetByIdAsync("PKG-XYZ"), Times.Once);

            QACollector.LogTestCase("VipPackage - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vip Package",
                TestCaseID = "TC-VIP-DEL-04",
                Description = "GetByIdAsync is invoked with the exact Id from the command",
                ExpectedResult = "GetByIdAsync(\"PKG-XYZ\") called once",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Command.Id = \"PKG-XYZ\"",
                    "GetByIdAsync called with exactly that Id"
                }
            });
        }

        // ── TC-05: UpdateAsync nhận đúng package object ──────────────────
        [Fact]
        public async Task Handle_ValidPackage_ShouldPassCorrectObjectToUpdateAsync()
        {
            // Arrange
            var package = MockVipPackageRepository.GetSamplePackage("PKG-001");
            package.IsActive = true;

            Tokki.Domain.Entities.VipPackage? passedToUpdate = null;
            var mockRepo = MockVipPackageRepository.GetMock(returnedPackage: package);
            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.VipPackage>()))
                    .Callback<Tokki.Domain.Entities.VipPackage>(p => passedToUpdate = p)
                    .Returns(Task.CompletedTask);

            var command = new DeleteVipPackageCommand { Id = "PKG-001" };
            var handler = CreateHandler(repo: mockRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            passedToUpdate.Should().NotBeNull();
            passedToUpdate!.Id.Should().Be("PKG-001");
            passedToUpdate.IsActive.Should().BeFalse();

            QACollector.LogTestCase("VipPackage - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vip Package",
                TestCaseID = "TC-VIP-DEL-05",
                Description = "UpdateAsync receives the same package object with IsActive=false",
                ExpectedResult = "UpdateAsync called with package.Id=\"PKG-001\" and IsActive=false",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Captured via Callback",
                    "Package.Id = \"PKG-001\"",
                    "Package.IsActive = false"
                }
            });
        }

        // ── TC-06: AddAsync không bao giờ được gọi khi xóa ──────────────
        [Fact]
        public async Task Handle_ValidPackage_ShouldNeverCallAddAsync()
        {
            // Arrange
            var package = MockVipPackageRepository.GetSamplePackage();
            var mockRepo = MockVipPackageRepository.GetMock(returnedPackage: package);

            var command = new DeleteVipPackageCommand { Id = "PKG-001" };
            var handler = CreateHandler(repo: mockRepo);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockRepo.Verify(x => x.AddAsync(It.IsAny<Tokki.Domain.Entities.VipPackage>()), Times.Never);

            QACollector.LogTestCase("VipPackage - Delete", new TestCaseDetail
            {
                FunctionGroup = "Delete Vip Package",
                TestCaseID = "TC-VIP-DEL-06",
                Description = "Delete flow must never call AddAsync",
                ExpectedResult = "IsSuccess=true, AddAsync Times.Never",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid package found",
                    "AddAsync never called (soft delete, not create)"
                }
            });
        }
    }
}