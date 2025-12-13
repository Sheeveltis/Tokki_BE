using MediatR;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Comments.DTOs;
public class CreateCommentCommand : IRequest<OperationResult<CommentDTO>>
{
    public string BlogId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;
}