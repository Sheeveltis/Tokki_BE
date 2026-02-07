using System.Data;
using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Queries.GetAccount
{
    public class GetAllAccountsQuery : IRequest<OperationResult<PagedResult<AccountDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public AccountStatus? Status { get; set; }
        public AccountRole? Role { get; set; }

        public VipStatus? VipStatus { get; set; }

        // Search
        public string? SearchName { get; set; }
        public string? SearchEmail { get; set; }
        public string? SearchPhone { get; set; }
    }

}
