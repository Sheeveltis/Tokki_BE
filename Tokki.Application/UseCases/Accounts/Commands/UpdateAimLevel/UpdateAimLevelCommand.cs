using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using System.Text.Json.Serialization;

namespace Tokki.Application.UseCases.Accounts.Commands.UpdateAimLevel
{
    public class UpdateAimLevelCommand : IRequest<OperationResult<bool>>
    {
        [JsonIgnore]
        public string? UserId { get; set; }
        
        public TopicLevel AimLevel { get; set; }
    }
}
