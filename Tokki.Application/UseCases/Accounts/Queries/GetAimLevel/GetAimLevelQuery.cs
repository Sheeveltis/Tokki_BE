using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Queries.GetAimLevel
{
    public class GetAimLevelQuery : IRequest<OperationResult<int?>>
    {
        public string UserId { get; set; } = string.Empty;
    }
}
