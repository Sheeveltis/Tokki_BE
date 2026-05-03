using MediatR;
using System.Collections.Generic;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.LiveChat.DTOs;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetClosedSupportRooms
{
    public class GetClosedSupportRoomsQuery : IRequest<OperationResult<List<ChatRoomDTO>>>
    {
        public int Days { get; set; } = 7;
        public string? Search { get; set; }
    }
}
