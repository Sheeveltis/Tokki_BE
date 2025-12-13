
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Favorite.Commands.AddFavoriteWord
{
    public class AddFavoriteWordCommand : IRequest<OperationResult<string>>
    {
        public string WordId { get; set; } = string.Empty;
        public string? MeaningId { get; set; }
        public string? Note { get; set; }
    }
}
