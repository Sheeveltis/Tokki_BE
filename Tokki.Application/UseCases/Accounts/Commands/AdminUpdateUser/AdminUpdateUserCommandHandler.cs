using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.AdminUpdateUser
{
    public class AdminUpdateUserCommandHandler : IRequestHandler<AdminUpdateUserCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;

        public AdminUpdateUserCommandHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<string>> Handle(AdminUpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByIdAsync(request.TargetUserId);
            if (user == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.UserNotFoundById });
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
            {
                var isDuplicate = await _accountRepository.IsPhoneNumberUsedByOtherUserAsync(request.PhoneNumber, request.TargetUserId);
                if (isDuplicate)
                {
                    return OperationResult<string>.Failure(new List<Error> { AppErrors.PhoneNumberDuplicated });
                }
                user.PhoneNumber = request.PhoneNumber;
            }

            if (request.Status == AccountStatus.Inactive)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.AccountInvalidStatusTransition });
            }

            user.FullName = request.FullName;
            user.Role = request.Role;
            user.Status = request.Status;
            user.AvatarUrl = request.AvatarUrl;

            if (request.DateOfBirth.HasValue)
            {
                user.DateOfBirth = request.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue);
            }

            user.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success($"Admin {request.AdminId} đã cập nhật người dùng {request.TargetUserId} thành công!");
        }
    }
}
