using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
namespace Tokki.Application.UseCases.Accounts.Queries.GetAccountDetailById
{
    // Tokki.Application/UseCases/Accounts/Queries/GetAccountDetailByIdQueryHandler.cs
   
        public class GetAccountDetailByIdQueryHandler : IRequestHandler<GetAccountDetailByIdQuery, OperationResult<AccountDetailDto>>
        {
            private readonly IAccountRepository _repository;

            public GetAccountDetailByIdQueryHandler(IAccountRepository repository)
            {
                _repository = repository;
            }

            public async Task<OperationResult<AccountDetailDto>> Handle(
                GetAccountDetailByIdQuery request,
                CancellationToken cancellationToken)
            {
                var account = await _repository.GetByIdAsync(request.UserId);

                if (account == null)
                {
                    return OperationResult<AccountDetailDto>.Failure(
                        AppErrors.AccountNotFound,
                        404
                    );
                }

                // Map tất cả các trường
                var dto = new AccountDetailDto
                {
                    UserId = account.UserId,
                    Email = account.Email,
                    PhoneNumber = account.PhoneNumber,
                    DateOfBirth = account.DateOfBirth ?? new DateTime(2000, 1, 1),
                    PasswordHash = account.PasswordHash,
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
                    FailedLoginCount = account.FailedLoginCount,
                    LockedUntil = account.LockedUntil,
                    LastLoginAt = account.LastLoginAt,
                    CreatedAt = account.CreatedAt,
                    UpdatedAt = account.UpdatedAt
                };

                return OperationResult<AccountDetailDto>.Success(
                    dto,
                    200,
                    "Lấy thông tin chi tiết tài khoản thành công"
                );
            }
        
    }
}
