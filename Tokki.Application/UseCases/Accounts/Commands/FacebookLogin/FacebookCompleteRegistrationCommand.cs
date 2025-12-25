using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.DTOs;

namespace Tokki.Application.UseCases.Accounts.Commands.FacebookLogin
{
    public class FacebookCompleteRegistrationCommand : IRequest<OperationResult<FacebookLoginResponse>>
    {
        public string AccessToken { get; set; } = string.Empty;

        public string FacebookId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string? Name { get; set; }
        public bool IsComfirmToMergeAcc { get; set; } = false;
    }
}
