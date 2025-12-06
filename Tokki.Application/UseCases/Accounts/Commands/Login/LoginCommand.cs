using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.DTOs;

namespace Tokki.Application.UseCases.Accounts.Commands.Login
{
    public class LoginCommand : IRequest<OperationResult<LoginResponse>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
