using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetChatHistory
{
    public class GetChatHistoryQuery : IRequest<OperationResult<List<ChatMessage>>>
    {
        public string RoomId { get; set; } = string.Empty;
    }
}
