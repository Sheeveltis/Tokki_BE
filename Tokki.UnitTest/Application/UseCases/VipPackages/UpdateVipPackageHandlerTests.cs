using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VipPackages.Commands.UpdateVipPackage;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.VipPackages
{
    public class UpdateVipPackageHandlerTests
    {
        private UpdateVipPackageHandler CreateHandler(
            Mock<IVipPackageRepository>? repo = null)
        {
            return new UpdateVipPackageHandler(
                (repo ?? MockVipPackageRepository.GetMock()).Object);
        }

        [Fact]
        public async Task Handle_PackageNotFound_ShouldReturnFailure()
        {
            var command = new UpdateVipPackageCommand
            {
                Id = "PKG-INVALID",
                Price = 99000,
                DurationDays = 30
            };

            var handler = CreateHandler(
                repo: MockVipPackageRepository.GetMock(returnedPackage: null));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("VipPackage - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vip Package",
                TestCaseID = "TC-VIP-UPD-01",
                Description = "Update gói VIP với ID không tồn tại",
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
        public async Task Handle_NegativePrice_ShouldReturnFailure()
        {
            var command = new UpdateVipPackageCommand
            {
                Id = "PKG-001",
                Price = -500,       // giá âm
                DurationDays = 30
            };

            var handler = CreateHandler(
                repo: MockVipPackageRepository.GetMock(
                    returnedPackage: MockVipPackageRepository.GetSamplePackage()));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("VipPackage - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vip Package",
                TestCaseID = "TC-VIP-UPD-02",
                Description = "Update Price thành giá âm",
                ExpectedResult = "Return Failure VipPackageInvalidPrice",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Price = -500 (invalid)",
                    "Return Failure"
                }
            });
        }

        [Fact]
        public async Task Handle_ValidData_ShouldUpdateFieldsAndReturn200()
        {
            var existingPackage = MockVipPackageRepository.GetSamplePackage();

            var command = new UpdateVipPackageCommand
            {
                Id = "PKG-001",
                Name = "VIP Premium Updated",
                Price = 199000,
                DurationDays = 60,
                IsActive = true
            };

            var mockRepo = MockVipPackageRepository.GetMock(
                returnedPackage: existingPackage);

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            existingPackage.Name.Should().Be("VIP Premium Updated");
            existingPackage.Price.Should().Be(199000);
            existingPackage.DurationDays.Should().Be(60);
            existingPackage.IsActive.Should().BeTrue();

            mockRepo.Verify(
                x => x.UpdateAsync(It.IsAny<Tokki.Domain.Entities.VipPackage>()),
                Times.Once);

            QACollector.LogTestCase("VipPackage - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vip Package",
                TestCaseID = "TC-VIP-UPD-03",
                Description = "Update gói VIP hợp lệ → các fields được cập nhật đúng",
                ExpectedResult = "Return Success, Name/Price/DurationDays/IsActive được cập nhật",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Valid Package Id",
                    "Valid Price và DurationDays",
                    "IsActive = true",
                    "UpdateAsync called once",
                    "Return Success"
                }
            });
        }

        [Fact]
        public async Task Handle_ZeroDurationDays_ShouldNotUpdateDurationDays()
        {
            // DurationDays = 0 → handler bỏ qua, giữ nguyên giá trị cũ
            var existingPackage = MockVipPackageRepository.GetSamplePackage();
            var originalDuration = existingPackage.DurationDays;

            var command = new UpdateVipPackageCommand
            {
                Id = "PKG-001",
                Price = 99000,
                DurationDays = 0    // boundary: <= 0 → bỏ qua
            };

            var handler = CreateHandler(
                repo: MockVipPackageRepository.GetMock(
                    returnedPackage: existingPackage));

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // DurationDays giữ nguyên
            existingPackage.DurationDays.Should().Be(originalDuration);

            QACollector.LogTestCase("VipPackage - Update", new TestCaseDetail
            {
                FunctionGroup = "Update Vip Package",
                TestCaseID = "TC-VIP-UPD-04",
                Description = "Update DurationDays = 0 (boundary) → bỏ qua, giữ nguyên giá trị cũ",
                ExpectedResult = "Return Success, DurationDays không thay đổi",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "DurationDays = 0 (boundary: <= 0 bị bỏ qua)",
                    "DurationDays giữ nguyên",
                    "Return Success"
                }
            });
        }
    }
}