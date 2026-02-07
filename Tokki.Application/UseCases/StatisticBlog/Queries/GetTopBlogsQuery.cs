using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.StatisticBlog.DTOs;

public class GetTopBlogsQuery : IRequest<OperationResult<List<TopBlogDTO>>>
{
    public int Count { get; set; } = 5;
}

public class GetTopBlogsQueryHandler : IRequestHandler<GetTopBlogsQuery, OperationResult<List<TopBlogDTO>>>
{
    private readonly IStatisticBlogRepository _repo;
    public GetTopBlogsQueryHandler(IStatisticBlogRepository repo) => _repo = repo;

    public async Task<OperationResult<List<TopBlogDTO>>> Handle(GetTopBlogsQuery request, CancellationToken token)
    {
        var blogs = await _repo.GetTopViewedBlogsAsync(request.Count, token);
        return OperationResult<List<TopBlogDTO>>.Success(blogs,200,OperationMessages.GetSuccess("Top blog nhiều lượt xem"));
    }
}