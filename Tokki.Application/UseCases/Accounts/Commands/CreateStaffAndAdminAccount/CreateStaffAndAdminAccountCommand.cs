using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount
{
    public class CreateStaffAndAdminAccountCommand : IRequest<OperationResult<string>>
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public AccountRole Role { get; set; } = AccountRole.Staff;

    }
}
