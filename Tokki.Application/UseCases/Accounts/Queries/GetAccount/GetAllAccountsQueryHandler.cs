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
            // Lấy dữ liệu phân trang
            var (items, totalCount) = await _repository.GetPagedAsync(
                request.PageNumber,
                request.PageSize
            );

            var dtos = new List<AccountDto>();

            foreach (var account in items)
            {
                // Đếm các thông tin liên quan
                var unlockedTitlesCount = await _repository.CountUnlockedTitlesAsync(account.UserId);
                var sessionsCount = await _repository.CountSessionsAsync(account.UserId);
                var socialLoginsCount = await _repository.CountSocialLoginsAsync(account.UserId);

                dtos.Add(new AccountDto
                {
                    UserId = account.UserId,
                    Email = account.Email,
                    PhoneNumber = account.PhoneNumber,
                    DateOfBirth = account.DateOfBirth,
                    FullName = account.FullName,
                    AvatarUrl = account.AvatarUrl,
                    Role = account.Role,
                    Status = account.Status,
                    VipExpirationDate = account.VipExpirationDate,
                    TotalXP = account.TotalXP,
                    CurrentStreak = account.CurrentStreak,
                    MaxStreak = account.MaxStreak,
                    LastStreakDate = account.LastStreakDate,
                    DailyStudySeconds = account.DailyStudySeconds,
                    CurrentTitleId = account.CurrentTitleId,
                    CurrentTitleName = account.CurrentTitle?.Name,
                    FailedLoginCount = account.FailedLoginCount,
                    LockedUntil = account.LockedUntil,
                    LastLoginAt = account.LastLoginAt,
                    CreatedAt = account.CreatedAt,
                    UpdatedAt = account.UpdatedAt,
                    UnlockedTitlesCount = unlockedTitlesCount,
                    SessionsCount = sessionsCount,
                    SocialLoginsCount = socialLoginsCount
                });
            }

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
