using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.LiveChat.Commands.JoinSupportChat
{
    public class JoinSupportChatCommand : IRequest<OperationResult<bool>>
    {
        [JsonIgnore]
        public string StaffId { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
    }
}
