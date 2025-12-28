using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class SocialLoginRepository : ISocialLoginRepository
    {
        private readonly TokkiDbContext _context;

        public SocialLoginRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<SocialLogin?> GetByProviderAsync(string provider, string providerUserId)
        {
            return await _context.SocialLogins
                .FirstOrDefaultAsync(x =>
                    x.Provider == provider &&
                    x.ProviderUserId == providerUserId);
        }

        public async Task AddAsync(SocialLogin socialLogin)
        {
            await _context.SocialLogins.AddAsync(socialLogin);
        }
    }

}
