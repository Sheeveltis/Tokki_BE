using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.DTOs;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class GamificationController : ControllerBase
    {
        private readonly IGamificationService _gamificationService;

        public GamificationController(IGamificationService gamificationService)
        {
            _gamificationService = gamificationService;
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> TrackStudyTime([FromBody] HeartbeatRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId)) return BadRequest("Vui lòng nhập UserId để test.");

            bool isStreakCompleted = await _gamificationService.TrackStudyTimeAsync(request.UserId, request.DurationInSeconds);

            return Ok(new
            {
                Message = "Tracking success",
                UserId = request.UserId,
                AddedSeconds = request.DurationInSeconds,
                IsStreakCompletedJustNow = isStreakCompleted
            });
        }
    }
}