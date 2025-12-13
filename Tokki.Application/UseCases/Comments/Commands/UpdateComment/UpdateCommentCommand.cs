using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Comments.DTOs;
using System.Text.Json.Serialization;

public class UpdateCommentCommand : IRequest<OperationResult<CommentDTO>>
{
    public string CommentId { get; set; } 
    public string Content { get; set; }

    [JsonIgnore]
    public string UserId { get; set; }
}