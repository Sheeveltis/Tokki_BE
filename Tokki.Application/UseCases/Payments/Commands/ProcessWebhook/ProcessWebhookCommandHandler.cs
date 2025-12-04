using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions; 
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
            _logger.LogInformation("--- NHẬN WEBHOOK SEPAY ---");
            _logger.LogInformation("Nội dung CK: {Content}", data.Content);
            _logger.LogInformation("Số tiền: {Amount}", data.TransferAmount);

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
                    CreatedAt = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)) // Giờ VN
                };

                await _paymentRepository.AddTransactionAsync(transaction);
                _logger.LogInformation("Đã lưu Transaction ID: {Id}", transaction.Id);

                string detectedPaymentId = ExtractPaymentId(data.Content);
                _logger.LogInformation("Payment ID tìm thấy: {Id}", detectedPaymentId);

                if (string.IsNullOrEmpty(detectedPaymentId))
                {
                    return OperationResult<string>.Success("Đã lưu Transaction nhưng không tìm thấy mã đơn hàng trong nội dung.");
                }

                var payment = await _paymentRepository.GetByIdAsync(detectedPaymentId);

                if (payment == null)
                {
                    _logger.LogWarning("Không tìm thấy Payment có ID {Id} trong database.", detectedPaymentId);
                    return OperationResult<string>.Success("Payment ID không tồn tại.");
                }

                if (payment.Status == PaymentStatus.Pending)
                {
                    if (data.TransferAmount >= payment.Amount)
                    {
                        payment.Status = PaymentStatus.Paid;
                        payment.PaidAt = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)); 
                        payment.TransactionId = transaction.Id; 

                        await _paymentRepository.UpdateAsync(payment);
                        _logger.LogInformation("UPDATE THÀNH CÔNG: Payment {Id} -> PAID", payment.Id);

                        return OperationResult<string>.Success("Thanh toán thành công.");
                    }
                    else
                    {
                        _logger.LogWarning("Số tiền không đủ. Yêu cầu: {Required}, Nhận: {Received}", payment.Amount, data.TransferAmount);
                    }
                }
                else
                {
                    _logger.LogInformation("Payment {Id} đã được xử lý trước đó (Status: {Status}).", payment.Id, payment.Status);
                }

                return OperationResult<string>.Success("Đã xử lý webhook.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý Webhook");
                return OperationResult<string>.Failure($"Lỗi Server: {ex.Message}");
            }
        }

        private string ExtractPaymentId(string content)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;
            var regex = new Regex(@"\b[a-zA-Z0-9_-]{10}\b");

            var match = regex.Match(content);
            if (match.Success)
            {
                return match.Value;
            }

            return string.Empty;
        }
    }
}