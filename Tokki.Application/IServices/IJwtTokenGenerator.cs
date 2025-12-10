using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IServices
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(Account user, DateTime expireAt);
        string GenerateForgotPasswordToken(string email);
    }
}
