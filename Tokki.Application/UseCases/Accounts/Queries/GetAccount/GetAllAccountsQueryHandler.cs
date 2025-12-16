using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;

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
            // Lấy tất cả accounts (hoặc dùng GetPagedAsync nếu muốn query trực tiếp từ DB)
            var (allAccounts, totalBeforeFilter) = await _repository.GetPagedAsync(1, int.MaxValue);

            // Apply filters
            var filteredAccounts = allAccounts.AsQueryable();

            // Filter by Status
            if (request.Status.HasValue)
            {
                filteredAccounts = filteredAccounts.Where(a => a.Status == request.Status.Value);
            }

            // Filter by Role
            if (request.Role.HasValue)
            {
                filteredAccounts = filteredAccounts.Where(a => a.Role == request.Role.Value);
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

            // Sort by CreatedAt (mới nhất lên đầu)
            filteredAccounts = filteredAccounts.OrderByDescending(a => a.CreatedAt);

            var totalCount = filteredAccounts.Count();

            // Apply pagination
            var pagedAccounts = filteredAccounts
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Map to DTOs
            var dtos = pagedAccounts.Select(account => new AccountDto
            {
                UserId = account.UserId,
                Email = account.Email,
                PhoneNumber = account.PhoneNumber,
                DateOfBirth = account.DateOfBirth,
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
                "Lấy danh sách tài khoản thành công"
            );
        }
    }
}