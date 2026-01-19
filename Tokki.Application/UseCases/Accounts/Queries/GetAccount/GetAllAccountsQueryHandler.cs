using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Queries.GetAccount
{
    public class GetAllAccountsQueryHandler : IRequestHandler<GetAllAccountsQuery, OperationResult<PagedResult<AccountDto>>>
    {
        private readonly IAccountRepository _repository;

        public GetAllAccountsQueryHandler(IAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<PagedResult<AccountDto>>> Handle(
            GetAllAccountsQuery request,
            CancellationToken cancellationToken)
        {
            var (allAccounts, _) = await _repository.GetPagedAsync(1, int.MaxValue);

            var filteredAccounts = allAccounts.AsQueryable();

            // CHỈ LẤY Role = User(0) và Vip(3)
            filteredAccounts = filteredAccounts.Where(a =>
                a.Role == AccountRole.User || a.Role == AccountRole.Vip);

            // Filter by Status
            if (request.Status.HasValue)
            {
                filteredAccounts = filteredAccounts.Where(a => a.Status == request.Status.Value);
            }

            // Search by Name
            if (!string.IsNullOrWhiteSpace(request.SearchName))
            {
                var searchName = request.SearchName.ToLower();
                filteredAccounts = filteredAccounts.Where(a =>
                    a.FullName != null && a.FullName.ToLower().Contains(searchName));
            }

            // Search by Email
            if (!string.IsNullOrWhiteSpace(request.SearchEmail))
            {
                var searchEmail = request.SearchEmail.ToLower();
                filteredAccounts = filteredAccounts.Where(a =>
                    a.Email != null && a.Email.ToLower().Contains(searchEmail));
            }

            // Search by Phone
            if (!string.IsNullOrWhiteSpace(request.SearchPhone))
            {
                filteredAccounts = filteredAccounts.Where(a =>
                    a.PhoneNumber != null && a.PhoneNumber.Contains(request.SearchPhone));
            }

            // Filter by VIP Status
            if (request.VipStatus.HasValue)
            {
                var now = DateTime.UtcNow;
                filteredAccounts = request.VipStatus.Value switch
                {
                    Domain.Enums.VipStatus.Active => filteredAccounts
                        .Where(a => a.VipExpirationDate.HasValue && a.VipExpirationDate.Value > now),

                    Domain.Enums.VipStatus.Expired => filteredAccounts
                        .Where(a => a.VipExpirationDate.HasValue && a.VipExpirationDate.Value <= now),

                    Domain.Enums.VipStatus.NoVip => filteredAccounts
                        .Where(a => !a.VipExpirationDate.HasValue),

                    _ => filteredAccounts
                };
            }

            // Sort newest first
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

            var pagedResult = PagedResult<AccountDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<AccountDto>>.Success(
                pagedResult,
                200,
                "Lấy danh sách tài khoản (User/Vip) thành công"
            );
        }
    }
}
