using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Queries.GetInternalUserVipAccounts
{
    public class GetInternalUserVipAccountsQueryHandler
        : IRequestHandler<GetInternalUserVipAccountsQuery, OperationResult<PagedResult<AccountDto>>>
    {
        private readonly IAccountRepository _repository;

        public GetInternalUserVipAccountsQueryHandler(IAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<PagedResult<AccountDto>>> Handle(
            GetInternalUserVipAccountsQuery request,
            CancellationToken cancellationToken)
        {
            var (allAccounts, _) = await _repository.GetPagedAsync(1, int.MaxValue);

            var filteredAccounts = allAccounts.AsQueryable();

            // Fixed role: User (0) + Vip (3)
            filteredAccounts = filteredAccounts.Where(a =>
                a.Role == AccountRole.User || a.Role == AccountRole.Vip);

            if (request.Status.HasValue)
                filteredAccounts = filteredAccounts.Where(a => a.Status == request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchName))
            {
                var s = request.SearchName.ToLower();
                filteredAccounts = filteredAccounts.Where(a => a.FullName != null && a.FullName.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchEmail))
            {
                var s = request.SearchEmail.ToLower();
                filteredAccounts = filteredAccounts.Where(a => a.Email != null && a.Email.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchPhone))
            {
                filteredAccounts = filteredAccounts.Where(a => a.PhoneNumber != null && a.PhoneNumber.Contains(request.SearchPhone));
            }

            if (request.VipStatus.HasValue)
            {
                var now = DateTime.UtcNow;
                filteredAccounts = request.VipStatus.Value switch
                {
                    VipStatus.Active => filteredAccounts.Where(a => a.VipExpirationDate.HasValue && a.VipExpirationDate.Value > now),
                    VipStatus.Expired => filteredAccounts.Where(a => a.VipExpirationDate.HasValue && a.VipExpirationDate.Value <= now),
                    VipStatus.NoVip => filteredAccounts.Where(a => !a.VipExpirationDate.HasValue),
                    _ => filteredAccounts
                };
            }

            filteredAccounts = filteredAccounts.OrderByDescending(a => a.CreatedAt);

            var totalCount = filteredAccounts.Count();

            var pagedAccounts = filteredAccounts
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var dtos = pagedAccounts.Select(account => new AccountDto
            {
                UserId = account.UserId,
                Email = account.Email,
                PhoneNumber = account.PhoneNumber,
                DateOfBirth = account.DateOfBirth ?? new DateTime(2000, 1, 1),
                FullName = account.FullName,
                AvatarUrl = account.AvatarUrl,
                Role = account.Role,
                Status = account.Status,
                VipExpirationDate = account.VipExpirationDate
            }).ToList();

            var pagedResult = PagedResult<AccountDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize);

            return OperationResult<PagedResult<AccountDto>>.Success(pagedResult, 200, "Lấy danh sách tài khoản (internal) thành công");
        }
    }
}
