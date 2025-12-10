using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Statistics.Queries;
namespace Tokki.WebAPI.Controllers

{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly ISender _sender;

        public StatisticsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var start = startDate ?? DateTime.Now.AddDays(-30);
            var end = endDate ?? DateTime.Now;
            var result = await _sender.Send(new GetDashboardOverviewQuery { StartDate = start, EndDate = end });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("chart")]
        public async Task<IActionResult> GetChart([FromQuery] int year)
        {
            if (year == 0) year = DateTime.Now.Year;
            var result = await _sender.Send(new GetRevenueChartQuery { Year = year });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("packages")]
        public async Task<IActionResult> GetPackagesStats([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var start = startDate ?? DateTime.Now.AddDays(-30);
            var end = endDate ?? DateTime.Now;
            var result = await _sender.Send(new GetRevenueByPackageQuery { StartDate = start, EndDate = end });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetTransactionsReportQuery
            {
                Search = search,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = pageSize
            };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
    }
}