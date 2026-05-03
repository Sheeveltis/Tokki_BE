using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetActiveSupportRoom
{
    public class GetActiveSupportRoomQueryHandler : IRequestHandler<GetActiveSupportRoomQuery, OperationResult<string?>>
    {
        private readonly IChatRoomRepository _repo;

        public GetActiveSupportRoomQueryHandler(IChatRoomRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<string?>> Handle(GetActiveSupportRoomQuery request, CancellationToken token)
        {
            var rooms = await _repo.GetUserRoomsAsync(request.UserId, token);
            
            // Tìm phòng support chưa đóng (IsClosed = false)
            var activeSupportRoom = rooms
                .Where(r => r.IsSupport && !r.IsClosed)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            return OperationResult<string?>.Success(activeSupportRoom?.ChatRoomId);
        }
    }
}
