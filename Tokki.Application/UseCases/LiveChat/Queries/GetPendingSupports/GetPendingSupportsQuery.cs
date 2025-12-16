using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.LiveChat.DTOs;

namespace Tokki.Application.UseCases.LiveChat.Queries.GetPendingSupports
{
    public class GetPendingSupportsQuery : IRequest<OperationResult<List<ChatRoomDTO>>> { }
}
