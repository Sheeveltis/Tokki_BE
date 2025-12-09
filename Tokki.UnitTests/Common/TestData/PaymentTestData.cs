using Tokki.Application.UseCases.Payments.Commands.CreatePayment;
using Tokki.Domain.Entities;

namespace Tokki.UnitTests.Common.TestData
{
    public static class PaymentTestData
    {
        public static CreatePaymentCommand GetValidCreateCommand()
        {
            return new CreatePaymentCommand
            {
                UserId = "user-test-01",
                VipPackageId = "vip-goi-thang" 
            };
        }

        public static VipPackage GetActiveVipPackage()
        {
            return new VipPackage
            {
                Id = "vip-goi-thang",
                Name = "Gói VIP 1 Tháng",
                Price = 50000,
                DurationDays = 30,
                IsActive = true,
                Description = "Gói cơ bản"
            };
        }

        public static VipPackage GetInactiveVipPackage()
        {
            return new VipPackage
            {
                Id = "vip-ngung-kd",
                Name = "Gói Cũ",
                Price = 20000,
                IsActive = false 
            };
        }
    }
}