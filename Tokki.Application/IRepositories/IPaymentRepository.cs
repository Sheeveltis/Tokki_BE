using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task AddTransactionAsync(Transaction transaction);
        Task<Payment?> GetByIdAsync(string id);
        Task UpdateAsync(Payment payment);
    }
}