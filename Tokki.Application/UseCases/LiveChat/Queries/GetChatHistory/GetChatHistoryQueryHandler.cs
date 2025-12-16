using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetChatHistory
{
    public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, OperationResult<List<LiveChatMessage>>>
    {
        private readonly IChatService _chatService; 

        public GetChatHistoryQueryHandler(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task<OperationResult<List<LiveChatMessage>>> Handle(GetChatHistoryQuery request, CancellationToken token)
        {
            try
            {
                var messages = await _chatService.GetHistoryAsync(request.RoomId);

                if (messages == null || !messages.Any())
                {
                    return OperationResult<List<LiveChatMessage>>.Success(new List<LiveChatMessage>());
                }

                var sortedMessages = messages.OrderBy(x => x.CreatedAt).ToList();

                return OperationResult<List<LiveChatMessage>>.Success(sortedMessages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI CHAT HISTORY]: {ex.Message}");
                return OperationResult<List<LiveChatMessage>>.Failure("Không thể tải lịch sử chat lúc này.");
            }
        }
    }
}
