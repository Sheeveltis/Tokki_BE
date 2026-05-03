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
        private readonly IIdGeneratorService _idGenerator;
        private readonly ILogger<ProcessWebhookCommandHandler> _logger;

        public ProcessWebhookCommandHandler(
            IPaymentRepository paymentRepository,
            IAccountRepository accountRepository,
            IVipPackageRepository vipPackageRepository,
            IIdGeneratorService idGenerator,
            ILogger<ProcessWebhookCommandHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _accountRepository = accountRepository;
            _vipPackageRepository = vipPackageRepository;
            _idGenerator = idGenerator;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
        {
            var data = request.Data;
            _logger.LogInformation("--- WEBHOOK SEPAY --- Content: {Content} | Amount: {Amount}", data.Content, data.TransferAmount);

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
                if (string.IsNullOrEmpty(detectedPaymentId))
                {
                    return OperationResult<string>.Success(AppErrors.PaymentInvalidContent.Description);
                }

                var payment = await _paymentRepository.GetByIdAsync(detectedPaymentId);
                if (payment == null)
                {
                    return OperationResult<string>.Success(AppErrors.PaymentNotFound.Description);
                }

                if (payment.Status == PaymentStatus.Pending)
                {
                    if (payment.ExpiresAt <= DateTimeOffset.UtcNow)
                    {
                        payment.Status = PaymentStatus.Expired;
                        await _paymentRepository.UpdateAsync(payment);
                        _logger.LogWarning("Webhook đến nhưng Payment {Id} đã hết hạn.", payment.Id);
                        return OperationResult<string>.Success("Payment đã hết hạn.");
                    }
                    if (data.TransferAmount >= payment.Amount)
                    {
                        payment.Status = PaymentStatus.Paid;
                        payment.PaidAt = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));
                        payment.TransactionId = transaction.Id;

                        var vipPackage = await _vipPackageRepository.GetByIdAsync(payment.VipPackageId);
                        if (vipPackage == null)
                        {
                            _logger.LogError("Critical: VipPackage {PkgId} not found for Payment {PayId}", payment.VipPackageId, payment.Id);
                            return OperationResult<string>.Failure(AppErrors.VipPackageNotFound);
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

                            _logger.LogInformation("Activated VIP for User {UserId}", user.UserId);
                        }

                        await _paymentRepository.UpdateAsync(payment);
                        return OperationResult<string>.Success(OperationMessages.PaymentSuccess());
                    }
                    else
                    {
                        return OperationResult<string>.Success(
                            OperationMessages.InsufficientAmount(data.TransferAmount, payment.Amount)
                        );
                    }
                }

                return OperationResult<string>.Success(AppErrors.PaymentAlreadyProcessed.Description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook Processing Error");
                return OperationResult<string>.Failure(AppErrors.ServerError, 500);
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