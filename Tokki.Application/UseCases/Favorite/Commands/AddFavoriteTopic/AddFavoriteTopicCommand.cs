
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Favorite.Commands.AddFavoriteTopic
{
    public class AddFavoriteTopicCommand : IRequest<OperationResult<string>>
    {
        public string TopicId { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
