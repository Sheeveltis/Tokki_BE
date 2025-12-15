using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly TokkiDbContext _context;

        public PaymentRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
        }

        public async Task<Payment?> GetByIdAsync(string id)
        {
            return await _context.Payments
                .Include(p => p.Transaction) 
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }
        public async Task<List<Payment>> GetByUserIdAsync(string userId)
        {
            return await _context.Payments
                .Include(p => p.Transaction) 
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt) 
                .ToListAsync();
        }
    }
}