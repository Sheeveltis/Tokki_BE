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
            var (pagedAccounts, totalCount) = await _repository.GetPagedWithSearchAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchText,
                request.Status,
                request.Role,
                request.VipStatus);

            // Map to DTOs
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
                "Lấy danh sách tài khoản thành công"
            );
        }
    }
}