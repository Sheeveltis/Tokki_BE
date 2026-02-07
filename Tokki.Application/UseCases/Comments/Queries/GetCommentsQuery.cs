using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Comments.DTOs;

public class GetCommentsQuery : IRequest<OperationResult<List<CommentDTO>>>
{
    public string BlogId { get; set; } = string.Empty;
}