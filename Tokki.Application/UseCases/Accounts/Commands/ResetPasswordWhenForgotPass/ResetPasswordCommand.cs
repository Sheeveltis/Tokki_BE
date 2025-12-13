using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Accounts.Commands.ResetPassword
{
    // UseCases/Accounts/Commands/ForgotPassword/ResetPassword/ResetPasswordCommand.cs
    public class ResetPasswordCommand : IRequest<OperationResult<string>>
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
