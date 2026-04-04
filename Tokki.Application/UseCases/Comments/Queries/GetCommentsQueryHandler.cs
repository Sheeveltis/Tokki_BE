using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Comments.DTOs;

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, OperationResult<List<CommentDTO>>>
{
    private readonly ICommentRepository _repo;

    public GetCommentsQueryHandler(ICommentRepository repo)
    {
        _repo = repo;
    }

    public async Task<OperationResult<List<CommentDTO>>> Handle(GetCommentsQuery request, CancellationToken token)
    {
        var allComments = await _repo.GetByBlogIdAsync(request.BlogId, token);

        if (!allComments.Any())
            return OperationResult<List<CommentDTO>>.Success(new List<CommentDTO>());

        var allDtos = allComments.Select(c => new CommentDTO
        {
            Id = c.CommentId,
            Content = c.IsDeleted ? "Bình luận này đã bị xóa." : c.Content,
            IsDeleted = c.IsDeleted,
            IsEdited = c.UpdatedAt.HasValue,
            CreatedAt = c.CreatedAt,
            UserId = c.UserId,
            AuthorName = c.User?.FullName ?? "Người dùng ẩn danh",
            AuthorAvatar = c.User?.AvatarUrl ?? "",
            Name = c.User?.CurrentTitle?.Name,
            ColorHex = c.User?.CurrentTitle?.ColorHex,
            IconUrl = c.User?.CurrentTitle?.IconUrl,
            ParentId = c.ParentId,
            Replies = new List<CommentDTO>()
        }).ToList();

        var lookup = allDtos.ToDictionary(c => c.Id);
        var rootComments = new List<CommentDTO>();

        foreach (var dto in allDtos)
        {
            if (string.IsNullOrEmpty(dto.ParentId))
            {
                rootComments.Add(dto);
            }
            else
            {
                if (lookup.TryGetValue(dto.ParentId, out var parentDto))
                {
                    parentDto.Replies.Add(dto);
                }
                else
                {
                    rootComments.Add(dto);
                }
            }
        }

        return OperationResult<List<CommentDTO>>.Success(rootComments);
    }
}