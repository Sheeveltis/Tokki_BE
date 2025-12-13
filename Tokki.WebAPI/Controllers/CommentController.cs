using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech.Transcription;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class CommentController : ControllerBase
{
    private readonly ISender _sender;

    public CommentController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize] 
    public async Task<IActionResult> Create([FromBody] CreateCommentCommand command)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        command.UserId = userId!;
        var result = await _sender.Send(command);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{blogId}")]
    public async Task<IActionResult> GetByBlog(string blogId)
    {
        var query = new GetCommentsQuery { BlogId = blogId };
        var result = await _sender.Send(query);
        return StatusCode(result.StatusCode, result);
    }

}