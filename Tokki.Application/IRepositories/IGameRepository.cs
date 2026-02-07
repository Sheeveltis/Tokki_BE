using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IGameRepository
    {
        /// <summary>
        /// Lấy danh sách game cho user, phân trang.
        /// Nên chỉ trả về các game đang Active.
        /// </summary>
        /// <param name="pageNumber">Trang hiện tại (bắt đầu từ 1)</param>
        /// <param name="pageSize">Số item mỗi trang</param>
        /// <param name="searchTerm">Tìm kiếm theo tên game (có thể null)</param>
        /// <param name="gameType">Lọc theo loại game (có thể null)</param>
        /// <returns>(items, totalCount)</returns>
        Task<(IReadOnlyList<Game> Items, int TotalCount)> GetPagedForUserAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            GameType? gameType
        );
        Task<Game?> GetByIdAsync(string gameId);

    }
}
