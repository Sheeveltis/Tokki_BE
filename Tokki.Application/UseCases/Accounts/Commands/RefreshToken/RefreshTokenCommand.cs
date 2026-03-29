// Application/UseCases/Accounts/Commands/RefreshToken/RefreshTokenCommand.cs
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.DTOs;

namespace Tokki.Application.UseCases.Accounts.Commands.RefreshToken
{
    public class RefreshTokenCommand : IRequest<OperationResult<LoginResponse>>
    {
        public string RawRefreshToken { get; set; } = string.Empty;
    }
}