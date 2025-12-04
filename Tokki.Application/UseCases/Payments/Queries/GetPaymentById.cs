using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Payments.Queries.GetPaymentById
{
    public class GetPaymentByIdQuery : IRequest<OperationResult<Payment>>
    {
        public string Id { get; set; }
    }
}