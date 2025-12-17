using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.DTOs;

namespace Tokki.Application.UseCases.Accounts.Commands.GoogleLogin
{
    public class GoogleLoginCommand : IRequest<OperationResult<LoginResponse>>
    {
        public Boolean IsComfirmToMergeAcc { get; set; } = false;
        public string IdToken { get; set; } = string.Empty;
    }
}
