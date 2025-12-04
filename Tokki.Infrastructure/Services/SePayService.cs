using Microsoft.Extensions.Configuration;
using Tokki.Application.IServices;

namespace Tokki.Infrastructure.Services
{
    public class SePayService : ISePayService
    {
        private readonly IConfiguration _configuration;

        public SePayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateQrUrl(string paymentId, decimal amount, string description)
        {
            var sepayConfig = _configuration.GetSection("SepaySettings");

            var bankName = sepayConfig["BankName"] ?? "OCB";
            var bankAccount = sepayConfig["AccountNumber"] ?? "0037100025583005";
            var subAccount = sepayConfig["SubAccount"]; 

            var accountToUse = !string.IsNullOrEmpty(subAccount) ? subAccount : bankAccount;

            var template = "compact"; 


            var content = paymentId;

            var url = $"https://qr.sepay.vn/img?bank={bankName}&acc={accountToUse}&template={template}&amount={amount}&des={content}";

            return url;
        }
    }
}