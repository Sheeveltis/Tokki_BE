using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface ISocialLoginRepository
    {
        Task<SocialLogin?> GetByProviderAsync(string provider, string providerUserId);
        Task AddAsync(SocialLogin socialLogin);
    }

}
