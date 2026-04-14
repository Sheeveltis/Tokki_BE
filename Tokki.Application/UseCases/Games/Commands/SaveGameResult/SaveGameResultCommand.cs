using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Games.Commands.SaveGameResult
{
    public class SaveGameResultCommand : IRequest<OperationResult<bool>>
    {
        public GameType GameType { get; set; }
        public string? TopicId { get; set; }
        public int Score { get; set; }
        public GameDifficulty GameDifficulty { get; set; }


        // UserId lấy từ token, không nhận từ client
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}
