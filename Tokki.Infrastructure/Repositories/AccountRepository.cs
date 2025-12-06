using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly TokkiDbContext _context;

        public AccountRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            // Kiểm tra email không phân biệt hoa thường
            return await _context.Accounts
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task AddAsync(Account user)
        {
            await _context.Accounts.AddAsync(user);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        // HÀM LẤY USER
        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        // HÀM LƯU SESSION
        public async Task AddSessionAsync(Session session)
        {
            await _context.Session.AddAsync(session);
        }

       
    }
}