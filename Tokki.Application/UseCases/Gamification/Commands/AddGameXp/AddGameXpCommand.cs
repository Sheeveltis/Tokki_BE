using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.Gamification.Commands.AddGameXp
{
    public class AddGameXpCommand : IRequest<OperationResult<AddGameXpResultDto>>
    {
        [JsonIgnore]
        public string? UserId { get; set; }
        public long Amount { get; set; }
        public XpSource Source { get; set; }
    }
}
