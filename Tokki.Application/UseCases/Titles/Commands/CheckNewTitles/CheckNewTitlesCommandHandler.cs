using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using System.Collections.Generic;

namespace Tokki.Application.UseCases.Titles.Commands.CheckNewTitles
{
    public class CheckNewTitlesCommandHandler : IRequestHandler<CheckNewTitlesCommand, OperationResult<List<Title>>>
    {
        private readonly IUserTitleService _userTitleService;

        public CheckNewTitlesCommandHandler(IUserTitleService userTitleService)
        {
            _userTitleService = userTitleService;
        }

        public async Task<OperationResult<List<Title>>> Handle(CheckNewTitlesCommand request, CancellationToken cancellationToken)
        {
            var newlyUnlocked = await _userTitleService.CheckAndUnlockLevelTitlesAsync(request.UserId);
            
            return OperationResult<List<Title>>.Success(newlyUnlocked, 200, 
                newlyUnlocked.Any() ? $"Chúc mừng! Bạn đã mở khóa {newlyUnlocked.Count} danh hiệu mới." : "Không có danh hiệu mới được mở khóa.");
        }
    }
}
