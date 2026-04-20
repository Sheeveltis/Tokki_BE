using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.StatisticPayment.Queries.GetStatisticPayment;
using Tokki.Application.UseCases.StatisticPayment.Queries.GetStatisticPaymentOverview;
using Tokki.Application.UseCases.StatisticPayment.Queries.GetStatisticPaymentChart;
using Tokki.Application.UseCases.StatisticPayment.Queries.GetVipPackageLookup;
using Tokki.Application.UseCases.StatisticPayment.Queries.GetPackageDistribution;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class StatisticsPaymentController : ControllerBase
    {
        private readonly ISender _sender;

        public StatisticsPaymentController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentStatistics([FromQuery] GetStatisticPaymentQuery query)
        {
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("packages-lookup")]
        public async Task<IActionResult> GetVipPackagesLookup()
        {
            var result = await _sender.Send(new GetVipPackageLookupQuery());
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var start = startDate ?? DateTime.Now.AddDays(-30);
            var end = endDate ?? DateTime.Now;
            var result = await _sender.Send(new GetStatisticPaymentOverviewQuery { StartDate = start, EndDate = end });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("chart")]
        public async Task<IActionResult> GetChart([FromQuery] int year)
        {
            if (year == 0) year = DateTime.Now.Year;
            var result = await _sender.Send(new GetStatisticPaymentChartQuery { Year = year });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("package-distribution")]
        public async Task<IActionResult> GetPackageDistribution([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var start = startDate ?? DateTime.Now.AddDays(-30);
            var end = endDate ?? DateTime.Now;
            var result = await _sender.Send(new GetPackageDistributionQuery { StartDate = start, EndDate = end });
            return StatusCode(result.StatusCode, result);
        }
    }
}
