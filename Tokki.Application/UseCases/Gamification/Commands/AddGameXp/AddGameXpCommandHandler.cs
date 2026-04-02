using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Gamification.Commands.AddGameXp;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Application.Common.Helpers;

namespace Tokki.Application.UseCases.Gamification.Commands.AddGameXp
{
    public class AddGameXpCommandHandler : IRequestHandler<AddGameXpCommand, OperationResult<AddGameXpResultDto>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IUserTitleService _userTitleService;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IUserXpHistoryRepository _userXpHistoryRepository;
        private readonly IIdGeneratorService _idGenerator;

        public AddGameXpCommandHandler(
            IAccountRepository accountRepository,
            IUserTitleService userTitleService,
            ISystemConfigRepository systemConfigRepository,
            IUserXpHistoryRepository userXpHistoryRepository,
            IIdGeneratorService idGenerator)
        {
            _accountRepository = accountRepository;
            _userTitleService = userTitleService;
            _systemConfigRepository = systemConfigRepository;
            _userXpHistoryRepository = userXpHistoryRepository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<AddGameXpResultDto>> Handle(AddGameXpCommand command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(command.UserId))
                return OperationResult<AddGameXpResultDto>.Failure("UserId không hợp lệ.");

            var user = await _accountRepository.GetByIdAsync(command.UserId);
            if (user == null)
                return OperationResult<AddGameXpResultDto>.Failure($"Không tìm thấy user với id: {command.UserId}");

            long amount = command.Amount;
            long originalAmount = amount;
            int maxDailyAllowed = 150;

            // Limit for MiniGame
            var vietnamNow = DateTime.UtcNow.AddHours(7);
            if (command.Source == XpSource.MiniGame)
            {
                var today = vietnamNow.Date;
                var configValue = await _systemConfigRepository.GetValueByKeyAsync("MAX_MINI_GAME_XP_PER_SESSION");
                
                if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue.Trim(), out var configVal))
                {
                    maxDailyAllowed = configVal;
                }

                var alreadyEarnedToday = await _userXpHistoryRepository.GetTotalXpBySourceAndDateAsync(
                    command.UserId, XpSource.MiniGame, today);

                if (alreadyEarnedToday >= maxDailyAllowed)
                {
                    amount = 0;
                }
                else if (alreadyEarnedToday + amount > maxDailyAllowed)
                {
                    amount = maxDailyAllowed - alreadyEarnedToday;
                }
            }


            await _userXpHistoryRepository.AddAsync(new UserXpHistory
            {
                Id = _idGenerator.Generate(21),
                UserId = user.UserId,
                Amount = amount,
                Action = command.Source,
                CreatedAt = vietnamNow
            });

            int oldLevel = Tokki.Application.Common.Helpers.LevelEngine.GetLevel(user.TotalXP);

            user.TotalXP += amount;

            int newLevel = Tokki.Application.Common.Helpers.LevelEngine.GetLevel(user.TotalXP);
            bool isLevelUp = newLevel > oldLevel;

            // Check XP Titles
            var newlyUnlocked = await _userTitleService.CheckAndUnlockTitlesAsync(user.UserId, TitleRequirementType.XP, user.TotalXP);
            
            if (newlyUnlocked.Any() && string.IsNullOrEmpty(user.CurrentTitleId))
            {
                user.CurrentTitleId = newlyUnlocked.Last().TitleId;
            }

            user.UpdatedAt = vietnamNow;
            await _accountRepository.UpdateUserAsync(user);
            await _accountRepository.SaveChangesAsync(default);

            string message = amount > 0 ? $"Cộng thành công {amount} XP." : "Bạn không nhận được thêm XP do đã đạt giới hạn ngày.";
            if (command.Source == XpSource.MiniGame)
            {
                if (originalAmount > 0 && amount == 0)
                {
                    message = $"Bạn đã đạt giới hạn nhận XP từ MiniGame ngày hôm nay (Tối đa {maxDailyAllowed} XP).";
                }
                else if (amount < originalAmount)
                {
                    message = $"Bạn đã gần đạt giới hạn ngày. Chỉ cộng thêm {amount} XP (Tối đa {maxDailyAllowed}/ngày).";
                }
            }

            return OperationResult<AddGameXpResultDto>.Success(new AddGameXpResultDto
            {
                TotalXP = user.TotalXP,
                XpAdded = amount,
                IsLevelUp = isLevelUp,
                NewLevel = newLevel
            }, message: message);
        }
    }
}
