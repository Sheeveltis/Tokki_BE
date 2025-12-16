using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.LiveChat.DTOs;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetMyRooms
{
    public class GetMyRoomsQuery : IRequest<OperationResult<List<ChatRoomDTO>>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}
