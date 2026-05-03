using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetActiveSupportRoom
{
    public class GetActiveSupportRoomQuery : IRequest<OperationResult<string?>>
    {
        public string UserId { get; set; } = string.Empty;
    }
}
