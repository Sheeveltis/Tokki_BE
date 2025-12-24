using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Queries.MatchingCard
{
    public class GetMatchingCardsQuery : IRequest<OperationResult<List<MatchingCardDTO>>>
    {
        public string TopicId { get; set; } = string.Empty;
        public int Quantity { get; set; } = 10; // Mặc định lấy 10 từ (tương đương 20 thẻ)
    }
}
