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
                Description = "Xóa gói VIP với ID không tồn tại",
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
                Description = "Xóa gói VIP Active hợp lệ → IsActive = false (soft delete), return Success",
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
                Description = "Xóa gói VIP đã IsActive = false → idempotent, vẫn return Success",
                ExpectedResult = "Return Success, IsActive giữ = false",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "IsActive = false (boundary: đã inactive)",
                    "Idempotent → return Success"
                }
            });
        }
    }
}