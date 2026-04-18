using MediatR;
using System;
using System.Collections.Generic;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.StatisticPayment.DTOs;

namespace Tokki.Application.UseCases.StatisticPayment.Queries.GetPackageDistribution
{
    public class GetPackageDistributionQuery : IRequest<OperationResult<List<PackageDistributionDto>>>
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
