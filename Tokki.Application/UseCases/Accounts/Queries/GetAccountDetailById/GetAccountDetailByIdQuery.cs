using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.DTOs;

namespace Tokki.Application.UseCases.Accounts.Queries.GetAccountDetailById
{
    public class GetAccountDetailByIdQuery : IRequest<OperationResult<AccountDetailDto>>
    {
        public string UserId { get; set; } = string.Empty;
    }
}
