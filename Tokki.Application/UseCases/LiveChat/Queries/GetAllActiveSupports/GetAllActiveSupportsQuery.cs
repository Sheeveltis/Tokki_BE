using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.LiveChat.DTOs;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetAllActiveSupports
{
    public class GetAllActiveSupportsQuery : IRequest<OperationResult<List<ChatRoomDTO>>>
    {
    }
}
