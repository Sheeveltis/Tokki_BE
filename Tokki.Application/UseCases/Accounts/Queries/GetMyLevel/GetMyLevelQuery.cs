using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.DTOs;

namespace Tokki.Application.UseCases.Accounts.Queries.GetMyLevel
{
    public class GetMyLevelQuery : IRequest<OperationResult<GetMyLevelResponse>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}
