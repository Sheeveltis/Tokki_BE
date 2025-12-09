using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Payments.Commands.ProcessWebhook
{
    public class ProcessWebhookCommandHandler : IRequestHandler<ProcessWebhookCommand, OperationResult<string>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IVipPackageRepository _vipPackageRepository; 
        private readonly ISubscriptionRepository _subscriptionRepository; 
        private readonly IIdGeneratorService _idGenerator; 
        private readonly ILogger<ProcessWebhookCommandHandler> _logger;

        public ProcessWebhookCommandHandler(
            IPaymentRepository paymentRepository,
            IAccountRepository accountRepository,
            IVipPackageRepository vipPackageRepository,
            ISubscriptionRepository subscriptionRepository, 
            IIdGeneratorService idGenerator,
            ILogger<ProcessWebhookCommandHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _accountRepository = accountRepository;
            _vipPackageRepository = vipPackageRepository;
            _subscriptionRepository = subscriptionRepository;
            _idGenerator = idGenerator;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
        {
            var data = request.Data;
            _logger.LogInformation("--- NHẬN WEBHOOK SEPAY --- Content: {Content} | Amount: {Amount}", data.Content, data.TransferAmount);

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
                    CreatedAt = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7))
                };
                await _paymentRepository.AddTransactionAsync(transaction);

                string detectedPaymentId = ExtractPaymentId(data.Content);
                if (string.IsNullOrEmpty(detectedPaymentId)) return OperationResult<string>.Success("Không tìm thấy mã đơn hàng.");

                var payment = await _paymentRepository.GetByIdAsync(detectedPaymentId);
                if (payment == null) return OperationResult<string>.Success("Payment ID không tồn tại.");

                if (payment.Status == PaymentStatus.Pending)
                {
                    if (data.TransferAmount >= payment.Amount)
                    {
                        payment.Status = PaymentStatus.Paid;
                        payment.PaidAt = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));
                        payment.TransactionId = transaction.Id;

                        var vipPackage = await _vipPackageRepository.GetByIdAsync(payment.VipPackageId);
                        if (vipPackage == null)
                        {
                            _logger.LogError("Lỗi: Không tìm thấy gói VIP {PkgId} cho Payment {PayId}", payment.VipPackageId, payment.Id);
                            return OperationResult<string>.Failure("Lỗi dữ liệu gói VIP.");
                        }

                        var user = await _accountRepository.GetByIdAsync(payment.UserId);
                        if (user != null)
                        {
                            var currentTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));

                            var startDate = (user.Role == AccountRole.Vip && user.VipExpirationDate > currentTime)
                                            ? user.VipExpirationDate.Value
                                            : currentTime;

                            var endDate = startDate.AddDays(vipPackage.DurationDays);

                            user.VipExpirationDate = endDate;
                            user.Role = AccountRole.Vip;

                            await _accountRepository.UpdateUserAsync(user); 

                            var subscription = new Subscription
                            {
                                Id = _idGenerator.GenerateCustom(21), 
                                UserId = user.UserId,
                                VipPackageId = vipPackage.Id,   
                                PaymentId = payment.Id,         
                                StartDate = startDate.DateTime, 
                                EndDate = endDate.DateTime,     
                                Status = SubscriptionStatus.Active,
                                CreatedAt = DateTime.UtcNow
                            };

                            await _subscriptionRepository.AddAsync(subscription);

                            _logger.LogInformation("Đã tạo Subscription {SubId} cho User {UserId}", subscription.Id, user.UserId);
                        }

                        await _paymentRepository.UpdateAsync(payment);
                        return OperationResult<string>.Success("Thanh toán thành công. Đã kích hoạt VIP và lưu lịch sử.");
                    }
                    else
                    {
                        return OperationResult<string>.Success("Số tiền chuyển khoản không đủ.");
                    }
                }

                return OperationResult<string>.Success("Webhook đã được xử lý trước đó.");
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
            return match.Success ? match.Value : string.Empty;
        }
    }
}