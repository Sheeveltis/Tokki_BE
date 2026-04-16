using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.Common.Helpers;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Gamification.DTOs;
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
        private readonly ISender _sender;

        public GamificationController(IGamificationService gamificationService, 
                                      IAccountRepository accountRepository,
                                      ISender sender)
        {
            _gamificationService = gamificationService;
            _accountRepository = accountRepository;
            _sender = sender;
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

        [HttpGet("my-streak")]
        public async Task<IActionResult> GetMyStreak()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _gamificationService.GetStreakStatusAsync(userId);
            return Ok(result);
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

            var streakStatus = await _gamificationService.GetStreakStatusAsync(userId);
 
            return Ok(new
            {
                Level = currentLevel,
                TotalXP = user.TotalXP,
                XPInCurrentLevel = xpGainedInCurrentLevel,
                MaxXPOfLevel = xpRequiredForThisLevelRange,
                ProgressPercentage = Math.Round(((double)xpGainedInCurrentLevel / xpRequiredForThisLevelRange) * 100, 2),
                Streak = streakStatus.CurrentStreak,
                IsCompletedToday = streakStatus.IsCompletedToday,
                Title = user.CurrentTitle?.Name ?? "N/A"
            });
        }

        [HttpPost("add-xp")]
        public async Task<IActionResult> AddGameXp([FromBody] AddGameXpCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            command.UserId = userId; // Ensure UserId is from the token, not the body
            var result = await _sender.Send(command);
            
            return StatusCode(result.StatusCode, result);
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