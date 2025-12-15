using MediatR;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.LiveChat.Commands.CreateSupportChat
{
    public class CreateSupportChatCommand : IRequest<OperationResult<string>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}