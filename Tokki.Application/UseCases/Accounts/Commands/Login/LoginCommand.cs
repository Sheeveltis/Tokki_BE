using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Domain.Enums;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tokki.Application.UseCases.Accounts.Commands.Login
{
    public class LoginCommand : IRequest<OperationResult<LoginResponse>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;

        [JsonIgnore]
        public List<AccountRole>? AllowedRoles { get; set; }
    }
}
