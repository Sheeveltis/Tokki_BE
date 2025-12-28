using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Accounts.Commands.DeleteAccount
{
    public class DeleteAccountCommand : IRequest<OperationResult<string>>
    {
        [JsonIgnore]
        public string? UserId { get; set; }
    }
}
