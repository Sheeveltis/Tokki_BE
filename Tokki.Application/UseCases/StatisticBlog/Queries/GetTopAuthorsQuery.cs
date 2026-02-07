using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.StatisticBlog.DTOs;

public class GetTopAuthorsQuery : IRequest<OperationResult<List<TopAuthorDTO>>>
{
    public int Count { get; set; } = 5;
}

public class GetTopAuthorsQueryHandler : IRequestHandler<GetTopAuthorsQuery, OperationResult<List<TopAuthorDTO>>>
{
    private readonly IStatisticBlogRepository _repo;
    public GetTopAuthorsQueryHandler(IStatisticBlogRepository repo) => _repo = repo;

    public async Task<OperationResult<List<TopAuthorDTO>>> Handle(GetTopAuthorsQuery request, CancellationToken token)
    {
        var authors = await _repo.GetTopAuthorsAsync(request.Count, token);
        return OperationResult<List<TopAuthorDTO>>.Success(authors,200,OperationMessages.GetSuccess("Top người đăng bài nổi bật"));
    }
}