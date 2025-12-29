using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetChatHistory
{
    public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, OperationResult<List<ChatMessage>>>
    {
        private readonly IChatRepository _chatService; 

        public GetChatHistoryQueryHandler(IChatRepository chatService)
        {
            _chatService = chatService;
        }

        public async Task<OperationResult<List<ChatMessage>>> Handle(GetChatHistoryQuery request, CancellationToken token)
        {
            try
            {
                var messages = await _chatService.GetHistoryAsync(request.RoomId);

                if (messages == null || !messages.Any())
                {
                    return OperationResult<List<ChatMessage>>.Success(new List<ChatMessage>());
                }

                var sortedMessages = messages.OrderBy(x => x.CreatedAt).ToList();

                return OperationResult<List<ChatMessage>>.Success(sortedMessages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI CHAT HISTORY]: {ex.Message}");
                return OperationResult<List<ChatMessage>>.Failure("Không thể tải lịch sử chat lúc này.");
            }
        }
    }
}
