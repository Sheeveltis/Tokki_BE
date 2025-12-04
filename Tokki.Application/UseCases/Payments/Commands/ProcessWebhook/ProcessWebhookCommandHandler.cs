using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Payments.Commands.ProcessWebhook
{
    public class ProcessWebhookCommandHandler : IRequestHandler<ProcessWebhookCommand, OperationResult<string>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<ProcessWebhookCommandHandler> _logger;

        public ProcessWebhookCommandHandler(IPaymentRepository paymentRepository, ILogger<ProcessWebhookCommandHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
        {
            var data = request.Data;
            _logger.LogInformation("Nhận Webhook SePay: {Content} - {Amount}", data.Content, data.TransferAmount);

            try
            {
                var transaction = new Transaction
                {
                    Gateway = data.Gateway,
                    TransactionDate = DateTimeOffset.Parse(data.TransactionDate),
                    AccountNumber = data.AccountNumber,
                    SubAccount = data.SubAccount,
                    AmountIn = data.TransferType == "in" ? data.TransferAmount : 0,
                    AmountOut = data.TransferType == "out" ? data.TransferAmount : 0,
                    TransactionContent = data.Content,
                    ReferenceNumber = data.ReferenceCode,
                    Body = data.Description,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _paymentRepository.AddTransactionAsync(transaction);
            
                string detectedPaymentId = ExtractPaymentId(data.Content);

                if (string.IsNullOrEmpty(detectedPaymentId))
                {
                    return OperationResult<string>.Success("Lưu Transaction thành công, nhưng không tìm thấy Payment ID khớp.");
                }

                var payment = await _paymentRepository.GetByIdAsync(detectedPaymentId);

                if (payment != null && payment.Status == PaymentStatus.Pending)
                {
                    if (data.TransferAmount >= payment.Amount)
                    {
                        payment.Status = PaymentStatus.Paid;
                        payment.PaidAt = DateTimeOffset.UtcNow;
                        payment.TransactionId = transaction.Id; 

                        await _paymentRepository.UpdateAsync(payment);
                        return OperationResult<string>.Success("Thanh toán thành công.");
                    }
                }

                return OperationResult<string>.Success("Đã xử lý webhook.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý Webhook");
                return OperationResult<string>.Failure("Lỗi Server");
            }
        }

        private string ExtractPaymentId(string content)
        {
            return content; 
        }
    }
}