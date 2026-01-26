using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Queries.GetInternalUserVipAccounts
{
    public class GetInternalUserVipAccountsQuery : IRequest<OperationResult<PagedResult<AccountDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public AccountStatus? Status { get; set; }
        public VipStatus? VipStatus { get; set; }

        public string? SearchName { get; set; }
        public string? SearchEmail { get; set; }
        public string? SearchPhone { get; set; }
    }
}
