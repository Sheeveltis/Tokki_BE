using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;

namespace Tokki.Application.UseCases.Accounts.Queries.GetUserProfile
{
    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, OperationResult<UserProfileDto>>
    {
        private readonly IAccountRepository _accountRepository;

        public GetUserProfileQueryHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByIdAsync(request.UserId);

            if (user == null)
            {
                return OperationResult<UserProfileDto>.Failure(
                    new List<Error> { new Error("User.NotFound", "Không tìm thấy người dùng.") },
                    404,
                    "Không tìm thấy người dùng."
                );
            }

            var userDto = new UserProfileDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                DateOfBirth = DateOnly.FromDateTime(user.DateOfBirth),
                Role = user.Role,
                Status = user.Status,
                TotalXP = user.TotalXP
            };

            return OperationResult<UserProfileDto>.Success(userDto, 200);
        }
    }
}
