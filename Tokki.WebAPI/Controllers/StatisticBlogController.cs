using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticBlogController : ControllerBase
    {
        private readonly ISender _sender;

        public StatisticBlogController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var result = await _sender.Send(new GetDashboardStatsQuery());
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("top-blogs")]
        public async Task<IActionResult> GetTopBlogs([FromQuery] int count = 5)
        {
            var result = await _sender.Send(new GetTopBlogsQuery { Count = count });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("top-authors")]
        public async Task<IActionResult> GetTopAuthors([FromQuery] int count = 5)
        {
            var result = await _sender.Send(new GetTopAuthorsQuery { Count = count });
            return StatusCode(result.StatusCode, result);
        }
    }
}
