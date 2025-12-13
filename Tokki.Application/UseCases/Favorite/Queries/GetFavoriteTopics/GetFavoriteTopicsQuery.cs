using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Favorite.DTOs;
using Tokki.Domain.Enums;
namespace Tokki.Application.UseCases.Favorite.Queries.GetFavoriteTopics
{
    public class GetFavoriteTopicsQuery : IRequest<OperationResult<PagedResult<FavoriteTopicDto>>>
    {
        public string? SearchTerm { get; set; }
        public UserFavoriteTopicStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}