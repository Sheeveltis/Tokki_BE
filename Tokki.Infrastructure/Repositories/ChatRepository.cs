using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly TokkiDbContext _context;

        public ChatRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task CreateMessageAsync(ChatMessage message)
        {
            await _context.ChatMessages.AddAsync(message);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ChatMessage>> GetHistoryAsync(string roomId)
        {
            var messages = await _context.ChatMessages
                .AsNoTracking() 
                .Where(x => x.RoomId == roomId)
                .OrderByDescending(x => x.CreatedAt) 
                .Take(50) 
                .Include(x => x.Sender) 
                .ToListAsync();

            foreach (var msg in messages)
            {
                if (msg.Sender != null)
                {
                    msg.SenderName = msg.Sender.FullName;
                    msg.SenderAvatar = msg.Sender.AvatarUrl;
                }
                else if (msg.Type == Domain.Enums.ChatMessageType.System) 
                {
                    msg.SenderName = "Hệ thống";
                    msg.SenderAvatar = "";
                }
                else
                {
                    msg.SenderName = "Unknown User";
                }

                msg.Sender = null;
            }

            return messages;
        }
    }
}