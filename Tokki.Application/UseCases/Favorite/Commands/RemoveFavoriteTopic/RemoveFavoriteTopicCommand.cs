using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Favorite.Commands.RemoveFavoriteTopic
{
    public class RemoveFavoriteTopicCommand : IRequest<OperationResult<bool>>
    {
        public string TopicId { get; set; } = string.Empty;
        public bool ForceDelete { get; set; } = false;
    }
}
