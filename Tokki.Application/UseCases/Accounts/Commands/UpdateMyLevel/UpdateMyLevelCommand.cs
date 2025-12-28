using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.UpdateMyLevel
{
    public class UpdateMyLevelCommand : IRequest<OperationResult<bool>>
    {
        public TopicLevel? Level { get; set; }

        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}
