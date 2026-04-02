using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.VipPackages.Queries.GetAllVipPackages;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.VipPackages
{
    public class GetAllVipPackagesHandlerTests
    {
        private GetAllVipPackagesHandler CreateHandler(
            Mock<IVipPackageRepository>? repo = null)
        {
            return new GetAllVipPackagesHandler(
                (repo ?? MockVipPackageRepository.GetMock()).Object);
        }

        [Fact]
        public async Task Handle_AsAdmin_ShouldReturnAllPackagesIncludingInactive()
        {
            var query = new GetAllVipPackagesQuery { IsAdmin = true };

            var allPackages = MockVipPackageRepository.GetSamplePackageList();

            var mockRepo = MockVipPackageRepository.GetMock(returnedAll: allPackages);
            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2); // cả active lẫn inactive

            // Verify GetAllAsync được gọi với IsAdmin = true
            mockRepo.Verify(x => x.GetAllAsync(true), Times.Once);

            QACollector.LogTestCase("VipPackage - Get All", new TestCaseDetail
            {
                FunctionGroup = "Get All Vip Packages",
                TestCaseID = "TC-VIP-GAL-01",
                Description = "Admin lấy tất cả gói VIP → trả về cả Active và Inactive",
                ExpectedResult = "Return Success, Data.Count = 2 (gồm cả inactive)",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "IsAdmin = true",
                    "GetAllAsync(true) called",
                    "Return 2 packages (Active + Inactive)"
                }
            });
        }

        [Fact]
        public async Task Handle_AsUser_ShouldReturnOnlyActivePackages()
        {
            var query = new GetAllVipPackagesQuery { IsAdmin = false };

            // Giả lập repository chỉ trả về gói active khi IsAdmin = false
            var activeOnly = new List<Tokki.Domain.Entities.VipPackage>
            {
                MockVipPackageRepository.GetSamplePackage("PKG-001")
            };

            var mockRepo = MockVipPackageRepository.GetMock(returnedAll: activeOnly);
            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.Should().OnlyContain(p => p.IsActive);

            mockRepo.Verify(x => x.GetAllAsync(false), Times.Once);

            QACollector.LogTestCase("VipPackage - Get All", new TestCaseDetail
            {
                FunctionGroup = "Get All Vip Packages",
                TestCaseID = "TC-VIP-GAL-02",
                Description = "User thường lấy gói VIP → chỉ trả về gói Active",
                ExpectedResult = "Return Success, Data.Count = 1 (chỉ Active)",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "IsAdmin = false",
                    "GetAllAsync(false) called",
                    "Return only Active packages"
                }
            });
        }

        [Fact]
        public async Task Handle_RepositoryThrows_ShouldReturnFailure()
        {
            var query = new GetAllVipPackagesQuery { IsAdmin = false };

            var mockRepo = MockVipPackageRepository.GetMock();
            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<bool>()))
                    .ThrowsAsync(new Exception("DB connection lost"));

            var handler = CreateHandler(repo: mockRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();

            QACollector.LogTestCase("VipPackage - Get All", new TestCaseDetail
            {
                FunctionGroup = "Get All Vip Packages",
                TestCaseID = "TC-VIP-GAL-03",
                Description = "Repository throw exception → return Failure VipPackageFetchFailed",
                ExpectedResult = "Return Failure VipPackageFetchFailed",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Repository throws Exception",
                    "Return Failure"
                }
            });
        }
    }
}