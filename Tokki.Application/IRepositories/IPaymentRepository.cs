using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<Payment?> GetByIdAsync(string id);
    }
}