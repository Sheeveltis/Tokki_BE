using Tokki.Application.UseCases.VipPackages.Commands.CreateVipPackage;
using Tokki.Application.UseCases.VipPackages.Commands.DeleteVipPackage;
using Tokki.Application.UseCases.VipPackages.Commands.UpdateVipPackage;
using Tokki.Domain.Entities;

namespace Tokki.UnitTests.Common.TestData
{
    public static class VipPackageTestData
    {
        public static CreateVipPackageCommand GetCreateCommand()
        {
            return new CreateVipPackageCommand
            {
                Name = "Monthly VIP Package",
                Price = 50000,
                DurationDays = 30,
                Description = "Basic package"
            };
        }

        public static UpdateVipPackageCommand GetUpdateCommand(string id)
        {
            return new UpdateVipPackageCommand
            {
                Id = id,
                Name = "VIP Package Updated",
                Price = 60000,
                IsActive = true
            };
        }

        public static VipPackage GetVipPackage(string id, bool isActive = true)
        {
            return new VipPackage
            {
                Id = id,
                Name = "Standard VIP Package",
                Price = 50000,
                DurationDays = 30,
                IsActive = isActive,
                Description = "Original description"
            };
        }
    }
}