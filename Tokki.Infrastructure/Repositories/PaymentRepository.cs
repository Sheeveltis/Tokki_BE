using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private static readonly List<Payment> _mockDb = new();

        public Task AddAsync(Payment payment)
        {
            _mockDb.Add(payment);
            return Task.CompletedTask; 
        }

        public Task<Payment?> GetByIdAsync(string id)
        {
            var payment = _mockDb.FirstOrDefault(p => p.Id == id);
            return Task.FromResult(payment);
        }
    }
}