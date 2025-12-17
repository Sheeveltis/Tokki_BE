using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

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
            // 1. Tìm người dùng cần sửa
            var user = await _accountRepository.GetByIdAsync(request.TargetUserId);
            if (user == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.UserNotFoundById });
            }

            // 2. Kiểm tra trùng số điện thoại (nếu Admin có nhập số điện thoại mới)
            if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
            {
                var isDuplicate = await _accountRepository.IsPhoneNumberUsedByOtherUserAsync(request.PhoneNumber, request.TargetUserId);
                if (isDuplicate)
                {
                    return OperationResult<string>.Failure(new List<Error> { AppErrors.PhoneNumberDuplicated });
                }
                user.PhoneNumber = request.PhoneNumber;
            }

            // 3. Cập nhật các thông tin
            user.FullName = request.FullName;
            user.Role = request.Role;
            user.Status = request.Status;
            user.AvatarUrl = request.AvatarUrl;

            if (request.DateOfBirth.HasValue)
            {
                user.DateOfBirth = request.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue);
            }

            user.UpdatedAt = DateTime.UtcNow.AddHours(7);

            // 4. Lưu thay đổi
            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success($"Admin {request.AdminId} đã cập nhật người dùng {request.TargetUserId} thành công!");
        }
    }
}
