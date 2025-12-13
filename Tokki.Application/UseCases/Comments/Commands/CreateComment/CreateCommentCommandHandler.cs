using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Comments.DTOs;
using Tokki.Domain.Entities;

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, OperationResult<CommentDTO>>

{

    private readonly ICommentRepository _commentRepo;

    private readonly IIdGeneratorService _idGen;
    private readonly IBlogRepository _blogRepo;


    public CreateCommentCommandHandler(ICommentRepository commentRepo, IIdGeneratorService idGen, 
        IBlogRepository blogRepository)

    {

        _commentRepo = commentRepo;

        _idGen = idGen;
        _blogRepo = blogRepository;

    }
    public async Task<OperationResult<CommentDTO>> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        try
        {
             var blogExists = await _blogRepo.ExistsAsync(request.BlogId);
             if (!blogExists) return OperationResult<CommentDTO>.Failure(AppErrors.BlogNotFound, 404);

            if (!string.IsNullOrEmpty(request.ParentId))
            {
                var parent = await _commentRepo.GetByIdAsync(request.ParentId);
                if (parent == null)
                    return OperationResult<CommentDTO>.Failure(AppErrors.CommentNotFound, 404);

                if (parent.ParentId != null) request.ParentId = parent.ParentId;
            }

            var comment = new Comment
            {
                CommentId = _idGen.Generate(15),
                Content = request.Content,
                BlogId = request.BlogId,
                UserId = request.UserId,
                ParentId = request.ParentId,
                CreatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false
            };
            await _commentRepo.AddAsync(comment);
            await _commentRepo.SaveChangesAsync(cancellationToken);

            var response = new CommentDTO
            {
                Id = comment.CommentId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = request.UserId
            };

            return OperationResult<CommentDTO>.Success(response, 201, OperationMessages.CreateSuccess("Bình luận"));
        }
        catch (Exception ex)
        {
            var realErrorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            Console.WriteLine($"LOI CHI TIET: {realErrorMessage}");
            return OperationResult<CommentDTO>.Failure(AppErrors.ServerError, 500, ex.Message);
        }
    }
}