using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Accounts.Commands.SetVipRole
{
    public class SetVipRoleCommand : IRequest<OperationResult<bool>>
    {
        public string UserId { get; set; } = string.Empty;
        public int Days { get; set; }
    }
}
