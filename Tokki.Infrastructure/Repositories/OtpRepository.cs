// OtpRepository.cs
using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class OtpRepository : IOtpRepository
    {
        private readonly TokkiDbContext _context;

        public OtpRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<Otp?> GetByIdAsync(string id) 
        {
            return await _context.OtpCodes.FindAsync(id);
        }

        public async Task<Otp?> GetLatestValidOtpAsync(string email, OtpType type)
        {
            return await _context.OtpCodes
                .Where(o => o.Email == email && o.Type == type && o.Status == OtpStatus.Active)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(Otp otp)
        {
            await _context.OtpCodes.AddAsync(otp);
        }

        public Task UpdateAsync(Otp otp)
        {
            _context.OtpCodes.Update(otp);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(string id)
        {
            var otp = await GetByIdAsync(id);
            if (otp != null)
            {
                _context.OtpCodes.Remove(otp);
            }
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}