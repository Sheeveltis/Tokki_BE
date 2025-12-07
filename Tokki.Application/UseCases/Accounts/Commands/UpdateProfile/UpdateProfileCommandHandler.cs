using FluentValidation;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Accounts.Commands.UpdateProfile
{
    public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, OperationResult<string>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IValidator<UpdateProfileCommand> _validator;

        public UpdateProfileCommandHandler(IAccountRepository accountRepository, IValidator<UpdateProfileCommand> validator)
        {
            _accountRepository = accountRepository;
            _validator = validator;
        }

        public async Task<OperationResult<string>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate Input
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return OperationResult<string>.Failure(errors, 400);
            }

            // 2. Lấy User từ DB (Dựa vào ID lấy từ Token)
            if (string.IsNullOrEmpty(request.UserId))
            {
                return OperationResult<string>.Failure("Không xác định được người dùng.", 401);
            }

            var user = await _accountRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return OperationResult<string>.Failure("Người dùng không tồn tại.", 404);
            }

            // 3. Cập nhật thông tin (CHỈ cập nhật các trường cho phép)
            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.DateOfBirth = request.DateOfBirth;

            // Nếu có gửi AvatarUrl mới thì update, không thì giữ nguyên
            if (!string.IsNullOrEmpty(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl;
            }

            // 4. Cập nhật thời gian chỉnh sửa (Múi giờ +7)
            user.UpdatedAt = DateTime.UtcNow.AddHours(7);

            // 5. Lưu vào DB
            await _accountRepository.UpdateUserAsync(user); // Hàm này bạn đã có ở bước trước
            await _accountRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success("Cập nhật thông tin thành công!", 200);
        }
    }
}