using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Tokki.Application.UseCases.Titles.Commands.CheckDailyTitles
{
    public class CheckDailyTitlesCommandHandler : IRequestHandler<CheckDailyTitlesCommand, OperationResult<List<Title>>>
    {
        private readonly IUserTitleService _userTitleService;

        public CheckDailyTitlesCommandHandler(IUserTitleService userTitleService)
        {
            _userTitleService = userTitleService;
        }

        public async Task<OperationResult<List<Title>>> Handle(CheckDailyTitlesCommand request, CancellationToken cancellationToken)
        {
            var newlyUnlocked = await _userTitleService.CheckAndUnlockDailyTitlesAsync(request.UserId);
            
            return OperationResult<List<Title>>.Success(newlyUnlocked, 200, 
                newlyUnlocked.Any() ? $"Chúc mừng! Bạn đã mở khóa {newlyUnlocked.Count} danh hiệu mới." : "Không có danh hiệu hàng ngày mới được mở khóa.");
        }
    }
}
