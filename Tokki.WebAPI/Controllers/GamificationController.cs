using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.Common.Helpers;
using Tokki.Application.IRepositories;
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
        private readonly IAccountRepository _accountRepository;

        public GamificationController(IGamificationService gamificationService, IAccountRepository accountRepository)
        {
            _gamificationService = gamificationService;
            _accountRepository = accountRepository;
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

        [HttpGet("progress/{userId}")]
        public async Task<IActionResult> GetUserProgress(string userId)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user == null) return NotFound();

            int currentLevel = LevelEngine.GetLevel(user.TotalXP);

            long xpAtStartOfLevel = LevelEngine.GetTotalXpRequiredForLevel(currentLevel);

            long xpAtStartOfNextLevel = LevelEngine.GetTotalXpRequiredForLevel(currentLevel + 1);

            long xpGainedInCurrentLevel = user.TotalXP - xpAtStartOfLevel;
            long xpRequiredForThisLevelRange = xpAtStartOfNextLevel - xpAtStartOfLevel;

            return Ok(new
            {
                Level = currentLevel,
                TotalXP = user.TotalXP,
                XPInCurrentLevel = xpGainedInCurrentLevel,
                MaxXPOfLevel = xpRequiredForThisLevelRange,
                ProgressPercentage = Math.Round(((double)xpGainedInCurrentLevel / xpRequiredForThisLevelRange) * 100, 2),
                Streak = user.AchievedGoalStreak,
                Title = user.CurrentTitle?.Name ?? "N/A"
            });
        }
    }
}