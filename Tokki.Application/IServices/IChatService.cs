using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IServices
{
    public interface IChatService
    {
        Task CreateMessageAsync(LiveChatMessage message);
        Task<List<LiveChatMessage>> GetHistoryAsync(string roomId);

    }
}
