using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.UseCases.Blogs.Commands.ApproveBlog;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;
using Tokki.Application.UseCases.Blogs.Commands.DeleteBlog;
using Tokki.Application.UseCases.Blogs.Commands.IncreaseViewCount;
using Tokki.Application.UseCases.Blogs.Commands.RejectBlog;
using Tokki.Application.UseCases.Blogs.Commands.SubmitBlogForApproval;
using Tokki.Application.UseCases.Blogs.Commands.UpdateBlog;
using Tokki.Application.UseCases.Blogs.Queries;
using Tokki.Application.UseCases.Blogs.Queries.GetPagedBlogs;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly ISender _sender; 
        public BlogController(ISender sender)
        {
            _sender = sender;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetPagedBlogsQuery query)
        {
            // If user is not Admin/Staff/Moderator, filter to only Published blogs
            if (!(User.IsInRole("Admin") || User.IsInRole("Staff") || User.IsInRole("Moderator")))
            {
                query.Status = Domain.Enums.BlogStatus.Published;
            }

            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var query = new GetBlogByIdQuery { Id = id };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost()]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Create([FromBody] CreateBlogCommand command)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
            command.CreatedBy = userId!;
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPut("{id}")]
        [Authorize(Roles ="Admin, Staff")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateBlogCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("ID trên URL không khớp với ID trong body.");
            }

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("admin/delete/{id}")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Delete(string id)
        {
            var command = new DeleteBlogCommand { Id = id };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }


        /// <summary>
        /// Dành cho Author (Staff): Gửi bài viết đi phê duyệt
        /// </summary>
        [HttpPost("staff/submit-approval/{id}")]
        [Authorize(Roles ="Staff")] 
        public async Task<IActionResult> SubmitForApproval(string id)
        {
            var command = new SubmitBlogForApprovalCommand { BlogId = id };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Dành cho Moderator: Phê duyệt bài viết (Publish)
        /// </summary>
        [HttpPost("moderator/approve/{id}")]
        [Authorize(Roles = "Moderator")] 
        public async Task<IActionResult> Approve(string id)
        {
            var command = new ApproveBlogCommand { BlogId = id };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Dành cho Moderator: Từ chối bài viết (Reject)
        /// </summary>
        [HttpPost("moderator/reject")]
        [Authorize(Roles = "Moderator")] 
        public async Task<IActionResult> Reject([FromBody] RejectBlogCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        /// <summary>
        /// Tăng số lượt xem cho bài viết
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost("increase-view/{id}")]
        [AllowAnonymous] 
        public async Task<IActionResult> IncreaseViewCount(string id)
        {
            var command = new IncreaseViewCountCommand { BlogId = id };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}
