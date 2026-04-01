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
                Description = "Admin takes all VIP packages → returns both Active and Inactive",
                ExpectedResult = "Return Success, Data.Count = 2 (including inactive)",
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
                Description = "Users often take the VIP package → only return the Active package",
                ExpectedResult = "Return Success, Data.Count = 1 (Active only)",
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

        // ── TC-04: Repository trả về danh sách rỗng → thành công ────────
        [Fact]
        public async Task Handle_NoPackagesAvailable_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new GetAllVipPackagesQuery { IsAdmin = false };

            var mockRepo = MockVipPackageRepository.GetMock(returnedAll: new List<Tokki.Domain.Entities.VipPackage>());
            var handler = CreateHandler(repo: mockRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();

            QACollector.LogTestCase("VipPackage - Get All", new TestCaseDetail
            {
                FunctionGroup = "Get All Vip Packages",
                TestCaseID = "TC-VIP-GAL-04",
                Description = "No packages in repository → return success with empty list",
                ExpectedResult = "IsSuccess=true, Data is empty list",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "GetAllAsync returns empty list",
                    "Return success with Data = []"
                }
            });
        }

        // ── TC-05: GetAllAsync được gọi đúng 1 lần ──────────────────────
        [Fact]
        public async Task Handle_AnyQuery_ShouldCallGetAllAsyncExactlyOnce()
        {
            // Arrange
            var query = new GetAllVipPackagesQuery { IsAdmin = true };
            var mockRepo = MockVipPackageRepository.GetMock(returnedAll: MockVipPackageRepository.GetSamplePackageList());
            var handler = CreateHandler(repo: mockRepo);

            // Act
            await handler.Handle(query, CancellationToken.None);

            // Assert
            mockRepo.Verify(x => x.GetAllAsync(It.IsAny<bool>()), Times.Once);

            QACollector.LogTestCase("VipPackage - Get All", new TestCaseDetail
            {
                FunctionGroup = "Get All Vip Packages",
                TestCaseID = "TC-VIP-GAL-05",
                Description = "GetAllAsync is called exactly once per request regardless of IsAdmin flag",
                ExpectedResult = "GetAllAsync called Times.Once",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "Any valid query",
                    "GetAllAsync called exactly once"
                }
            });
        }

        // ── TC-06: Kiểm tra Data trả về chứa đúng số package ────────────
        [Fact]
        public async Task Handle_ThreePackagesInRepo_ShouldReturnAllThree()
        {
            // Arrange
            var query = new GetAllVipPackagesQuery { IsAdmin = true };

            var packages = new List<Tokki.Domain.Entities.VipPackage>
            {
                MockVipPackageRepository.GetSamplePackage("PKG-A"),
                MockVipPackageRepository.GetSamplePackage("PKG-B"),
                MockVipPackageRepository.GetSampleInactivePackage()
            };

            var mockRepo = MockVipPackageRepository.GetMock(returnedAll: packages);
            var handler = CreateHandler(repo: mockRepo);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            result.Data.Select(p => p.Id).Should().Contain(new[] { "PKG-A", "PKG-B" });

            QACollector.LogTestCase("VipPackage - Get All", new TestCaseDetail
            {
                FunctionGroup = "Get All Vip Packages",
                TestCaseID = "TC-VIP-GAL-06",
                Description = "Repository has 3 packages → result Data contains all 3",
                ExpectedResult = "Data.Count = 3, contains PKG-A, PKG-B",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string>
                {
                    "3 packages in repository (2 active + 1 inactive)",
                    "IsAdmin = true → all returned",
                    "Data.Count = 3"
                }
            });
        }
    }
}