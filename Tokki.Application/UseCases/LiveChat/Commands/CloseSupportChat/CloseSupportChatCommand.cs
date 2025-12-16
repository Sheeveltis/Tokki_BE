using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.LiveChat.Commands.CloseSupportChat
{
    public class CloseSupportChatCommand : IRequest<OperationResult<bool>>
    {
        public string RoomId { get; set; } = string.Empty;
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty; 
    }
}
