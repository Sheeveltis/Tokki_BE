using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Games.Queries.CheckUserPlayedLevel
{

    public class CheckUserPlayedLevelQuery : IRequest<OperationResult<bool>>
    {
        public string GameId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public GameDifficulty GameDifficulty { get; set; }

        // Lấy từ token, không nhận từ client
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}
