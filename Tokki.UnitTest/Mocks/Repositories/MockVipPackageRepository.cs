using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockVipPackageRepository
    {
        public static Mock<IVipPackageRepository> GetMock(
            VipPackage? returnedPackage = null,
            List<VipPackage>? returnedAll = null)
        {
            var mockRepo = new Mock<IVipPackageRepository>();

            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(returnedPackage);

            mockRepo.Setup(x => x.GetAllAsync(It.IsAny<bool>()))
                    .ReturnsAsync(returnedAll ?? new List<VipPackage>());

            mockRepo.Setup(x => x.AddAsync(It.IsAny<VipPackage>()))
                    .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<VipPackage>()))
                    .Returns(Task.CompletedTask);

            return mockRepo;
        }
      

        public static VipPackage GetSamplePackage(string id = "PKG-001")
        {
            return new VipPackage
            {
                Id = id,
                Name = "VIP Basic",
                PackageType = "Monthly",
                Price = 99000,
                DurationDays = 30,
                Description = "Gói cơ bản 1 tháng",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static VipPackage GetSampleInactivePackage(string id = "PKG-002")
        {
            return new VipPackage
            {
                Id = id,
                Name = "VIP Premium",
                PackageType = "Yearly",
                Price = 999000,
                DurationDays = 365,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
        }


        public static List<VipPackage> GetSamplePackageList()
        {
            return new List<VipPackage>
            {
                GetSamplePackage("PKG-001"),
                GetSampleInactivePackage("PKG-002")
            };
        }
    }
}