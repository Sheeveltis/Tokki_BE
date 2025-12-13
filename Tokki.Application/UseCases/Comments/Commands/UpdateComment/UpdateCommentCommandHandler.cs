using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Comments.DTOs;

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, OperationResult<CommentDTO>>
{
    private readonly ICommentRepository _commentRepo;

    public UpdateCommentCommandHandler(ICommentRepository commentRepo)
    {
        _commentRepo = commentRepo;
    }

    public async Task<OperationResult<CommentDTO>> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _commentRepo.GetByIdAsync(request.CommentId);

        if (comment == null || comment.IsDeleted)
        {
            return OperationResult<CommentDTO>.Failure(AppErrors.CommentNotFound, 404);
        }

        if (comment.UserId != request.UserId)
        {
            return OperationResult<CommentDTO>.Failure(AppErrors.Forbidden, 403);
        }

        comment.Content = request.Content;
        await _commentRepo.SaveChangesAsync(cancellationToken);

        var response = new CommentDTO
        {
            Id = comment.CommentId,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UserId = comment.UserId
        };

        return OperationResult<CommentDTO>.Success(response, 200, OperationMessages.UpdateSuccess("Bình luận"));
    }
}