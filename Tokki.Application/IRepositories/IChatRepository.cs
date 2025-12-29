using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IChatRepository
    {
        Task CreateMessageAsync(ChatMessage message);
        Task<List<ChatMessage>> GetHistoryAsync(string roomId);

    }
}
