using Tokki.Application.UseCases.Payments.Commands.CreatePayment;
using Tokki.Application.UseCases.Payments.Commands.ProcessWebhook;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

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

        public static ProcessWebhookCommand GetWebhookCommand(string content, decimal amount)
        {
            return new ProcessWebhookCommand(new SePayWebhookData
            {
                Gateway = "MBBank",
                TransactionDate = "2023-10-25 10:00:00",
                AccountNumber = "000000",
                Content = content,
                TransferAmount = amount,
                TransferType = "in",
                ReferenceCode = "REF123"
            });
        }

        public static Payment GetPayment(string id, decimal amount, PaymentStatus status)
        {
            return new Payment
            {
                Id = id,
                Amount = amount,
                Status = status,
                UserId = "user-vip-01",
                VipPackageId = "vip-goi-thang",
                Description = $"Thanh toan {id}"
            };
        }

        public static Account GetUser(string id, AccountRole role)
        {
            return new Account
            {
                UserId = id,
                Role = role,
                VipExpirationDate = role == AccountRole.Vip ? DateTimeOffset.UtcNow.AddDays(5) : null
            };
        }
    }
}