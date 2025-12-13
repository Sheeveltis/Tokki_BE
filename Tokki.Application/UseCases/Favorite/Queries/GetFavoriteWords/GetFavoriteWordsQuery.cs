using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Favorite.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Favorite.Queries.GetFavoriteWords
{
    public class GetFavoriteWordsQuery : IRequest<OperationResult<PagedResult<FavoriteWordDto>>>
    {
        public string? SearchTerm { get; set; }
        public UserFavoriteWordStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
