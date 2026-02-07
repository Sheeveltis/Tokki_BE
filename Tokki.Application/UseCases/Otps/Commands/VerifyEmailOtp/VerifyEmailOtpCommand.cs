using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Accounts.Commands.VerifyEmailOtp
{
    // Output là string thông báo thành công. 
    // Nếu bạn muốn trả về Token đăng nhập luôn thì đổi thành OperationResult<LoginResponse>
    public class VerifyEmailOtpCommand : IRequest<OperationResult<string>>
    {
        public string Email { get; set; }
        public string OtpCode { get; set; }
    }
}