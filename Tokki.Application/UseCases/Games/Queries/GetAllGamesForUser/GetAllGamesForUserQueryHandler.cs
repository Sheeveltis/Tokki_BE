using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Games.DTOs;

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
            var result = await _gameRepository.GetPagedForUserAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.GameType
            );

            var items = result.Items;
            var totalCount = result.TotalCount;

            var dtos = new List<GameForUserDto>();

            foreach (var game in items)
            {
                dtos.Add(new GameForUserDto
                {
                    GameId = game.GameId,
                    GameName = game.GameName,
                    GameType = game.GameType,
                    IsVip = game.IsVip,
                    ImgUrl = game.ImgUrl   // lấy ảnh ra đây
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
