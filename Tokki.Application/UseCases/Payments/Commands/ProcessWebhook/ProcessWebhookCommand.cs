using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Payments.Commands.ProcessWebhook
{
    public class SePayWebhookData
    {
        public int Id { get; set; }
        public string Gateway { get; set; }
        public string TransactionDate { get; set; }
        public string AccountNumber { get; set; }
        public string SubAccount { get; set; }
        public decimal TransferAmount { get; set; }
        public string TransferType { get; set; } 
        public string Content { get; set; }
        public string ReferenceCode { get; set; }
        public string Description { get; set; }
    }

    public class ProcessWebhookCommand : IRequest<OperationResult<string>>
    {
        public SePayWebhookData Data { get; set; }
    }
}