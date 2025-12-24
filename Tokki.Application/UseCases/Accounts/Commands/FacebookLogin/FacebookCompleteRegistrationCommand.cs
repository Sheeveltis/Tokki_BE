using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.DTOs;

namespace Tokki.Application.UseCases.Accounts.Commands.FacebookLogin
{
    public class FacebookCompleteRegistrationCommand : IRequest<OperationResult<LoginResponse>>
    {
        public string FacebookId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Lấy lại từ lần đầu login (không có email)
        public string Name { get; set; } = string.Empty;
        public string? Birthday { get; set; }
        public string? Gender { get; set; }

        // Chỉ dùng khi email đã tồn tại và cần merge
        public bool IsComfirmToMergeAcc { get; set; } = false;
    }
}
