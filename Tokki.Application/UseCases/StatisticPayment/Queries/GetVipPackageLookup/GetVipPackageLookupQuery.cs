using MediatR;
using System.Collections.Generic;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.StatisticPayment.DTOs;

namespace Tokki.Application.UseCases.StatisticPayment.Queries.GetVipPackageLookup
{
    public class GetVipPackageLookupQuery : IRequest<OperationResult<List<VipPackageLookupDto>>>
    {
    }
}
