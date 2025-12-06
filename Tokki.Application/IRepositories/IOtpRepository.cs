using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IOtpRepository
    {
        Task AddAsync(Otp otp);
        Task<Otp?> GetLatestValidOtpAsync(string email, OtpType type);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    }
}
