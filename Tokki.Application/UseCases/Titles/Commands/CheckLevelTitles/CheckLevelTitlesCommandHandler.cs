using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Tokki.Application.UseCases.Titles.Commands.CheckLevelTitles
{
    public class CheckLevelTitlesCommandHandler : IRequestHandler<CheckLevelTitlesCommand, OperationResult<List<Title>>>
    {
        private readonly IUserTitleService _userTitleService;

        public CheckLevelTitlesCommandHandler(IUserTitleService userTitleService)
        {
            _userTitleService = userTitleService;
        }

        public async Task<OperationResult<List<Title>>> Handle(CheckLevelTitlesCommand request, CancellationToken cancellationToken)
        {
            var newlyUnlocked = await _userTitleService.CheckAndUnlockLevelTitlesAsync(request.UserId);
            
            return OperationResult<List<Title>>.Success(newlyUnlocked, 200, 
                newlyUnlocked.Any() ? $"Chúc mừng! Bạn đã mở khóa {newlyUnlocked.Count} danh hiệu cấp độ mới." : "Không có danh hiệu cấp độ mới được mở khóa.");
        }
    }
}
