using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Accounts.Commands.FacebookLogin
{
    public class FacebookCompleteRegistrationCommand : IRequest<OperationResult<LoginResponse>>
    {
        public string FacebookId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
