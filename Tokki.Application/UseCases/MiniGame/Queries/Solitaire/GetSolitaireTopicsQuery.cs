using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Queries.Solitaire
{
    public class GetSolitaireTopicsQuery : IRequest<OperationResult<List<SolitaireTopicDTO>>>
    {
        public int Quantity { get; set; } = 20;
    }
}
