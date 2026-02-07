using FluentValidation;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Tokki.Application.UseCases.Accounts.Commands.UpdateProfile
{
    public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;

        public UpdateProfileCommandHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<string>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.UserUnauthorized });
            }
            
            var user = await _accountRepository.GetByIdAsync(request.UserId);


            if (user == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.UserNotFoundById });
            }
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                var isDuplicate = await _accountRepository.IsPhoneNumberUsedByOtherUserAsync(
                    request.PhoneNumber,
                    request.UserId!
                );

                if (isDuplicate)
                {
                    return OperationResult<string>.Failure(new List<Error> { AppErrors.PhoneNumberDuplicated });
                }

                user.PhoneNumber = request.PhoneNumber;
            }
            if (!string.IsNullOrEmpty(request.FullName))
            {
                user.FullName = request.FullName;
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user.PhoneNumber = request.PhoneNumber;
            }

            if (request.DateOfBirth.HasValue)
            {
                user.DateOfBirth = request.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue);
            }

            if (!string.IsNullOrEmpty(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl;
            }


            user.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success("Cập nhật thông tin thành công!", 200);
        }
    }
}