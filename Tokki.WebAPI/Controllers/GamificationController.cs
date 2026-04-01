using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.Common.Helpers;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Gamification.Commands.AddGameXp;

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

        [HttpGet("progress")]
        public async Task<IActionResult> GetUserProgress()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _accountRepository.GetByIdAsync(userId);
            if (user == null) return NotFound();

            int currentLevel = Tokki.Application.Common.Helpers.LevelEngine.GetLevel(user.TotalXP);

            long xpAtStartOfLevel = Tokki.Application.Common.Helpers.LevelEngine.GetTotalXpRequiredForLevel(currentLevel);
            long xpAtStartOfNextLevel = Tokki.Application.Common.Helpers.LevelEngine.GetTotalXpRequiredForLevel(currentLevel + 1);

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

        [HttpPost("game-xp")]
        public async Task<IActionResult> AddGameXp([FromBody] AddGameXpRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (request.Amount < 1 || request.Amount > 500)
                return BadRequest("Số XP không hợp lệ. Vui lòng nhập giá trị từ 1 đến 500.");

            var result = await _gamificationService.AddGameXpAsync(userId, request.Amount);
            return Ok(result);
        }

        [HttpGet("level-up-check")]
        public async Task<IActionResult> CheckLevelUp([FromQuery] long addedXp)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _accountRepository.GetByIdAsync(userId);
            if (user == null) return NotFound();

            int oldLevel = Tokki.Application.Common.Helpers.LevelEngine.GetLevel(user.TotalXP);
            int newLevel = Tokki.Application.Common.Helpers.LevelEngine.GetLevel(user.TotalXP + addedXp);
            bool isLevelUp = newLevel > oldLevel;

            return Ok(new
            {
                CurrentLevel = oldLevel,
                NewLevel = newLevel,
                IsLevelUp = isLevelUp,
                TotalXP = user.TotalXP,
                XpAfterAddition = user.TotalXP + addedXp,
                Message = isLevelUp ? $"Chúc mừng! Bạn đã lên cấp {newLevel}!" : "Bạn chưa đủ XP để lên cấp."
            });
        }
    }
}