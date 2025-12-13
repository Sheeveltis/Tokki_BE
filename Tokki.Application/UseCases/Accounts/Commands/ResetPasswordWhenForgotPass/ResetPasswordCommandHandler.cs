using MediatR;
using Tokki.Application.Common.Models; 
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Accounts.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;

        public ChangePasswordCommandHandler(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<string>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.UserNotFound });
            }

              bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash);

            if (!isPasswordCorrect)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.InvalidCredentials });
            
            }

            // 3. Hash mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // (Tùy chọn) Reset các biến khóa tài khoản nếu đổi pass thành công (giống mẫu của bạn)
            user.FailedLoginCount = 0;
            user.LockedUntil = null;

            // 4. Lưu DB
            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success("Đổi mật khẩu thành công!", 200);
        }
    }
}