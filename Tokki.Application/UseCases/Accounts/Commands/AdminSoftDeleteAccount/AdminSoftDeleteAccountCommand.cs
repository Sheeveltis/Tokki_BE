using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Accounts.Commands.AdminSoftDeleteAccount
{

    public class AdminSoftDeleteAccountCommand : IRequest<OperationResult<string>>
    {
        public string? TargetUserId { get; set; }

        [JsonIgnore]
        public string? AdminUserId { get; set; }
    }
}
