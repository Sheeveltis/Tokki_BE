using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Games.Commands.SaveGameResult
{
    public class SaveGameResultCommand : IRequest<OperationResult<bool>>
    {
        public string GameId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public int Score { get; set; }

        // UserId lấy từ token, không nhận từ client
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}
