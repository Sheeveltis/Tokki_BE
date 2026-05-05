using MediatR;
using System;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.MiniGame.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetWordlePaginatedQuery : IRequest<OperationResult<PagedResult<WordleAdminDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public DateOnly? Date { get; set; }
        public WordleLevel? Level { get; set; }
        public string? SearchTerm { get; set; }
    }
}
