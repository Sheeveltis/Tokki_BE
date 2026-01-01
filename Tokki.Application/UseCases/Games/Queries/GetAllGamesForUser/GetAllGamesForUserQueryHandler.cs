using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Games.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Games.Queries.GetAllGamesForUser
{
    public class GetAllGamesForUserQueryHandler
        : IRequestHandler<GetAllGamesForUserQuery, OperationResult<PagedResult<GameForUserDto>>>
    {
        private readonly IGameRepository _gameRepository;

        public GetAllGamesForUserQueryHandler(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<OperationResult<PagedResult<GameForUserDto>>> Handle(
            GetAllGamesForUserQuery request,
            CancellationToken cancellationToken)
        {
            // Lấy dữ liệu phân trang cho user:
            // Repository nên chỉ trả về các game Status = Active
            var (items, totalCount) = await _gameRepository.GetPagedForUserAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.GameType
            );

            var dtos = new List<GameForUserDto>();

            foreach (var game in items)
            {
                dtos.Add(new GameForUserDto
                {
                    GameId = game.GameId,
                    GameName = game.GameName,
                    GameType = game.GameType,
                    IsVip = game.IsVip
                });
            }

            var pagedResult = PagedResult<GameForUserDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<GameForUserDto>>.Success(
                pagedResult,
                200,
                "Lấy danh sách game thành công"
            );
        }
    }
}
