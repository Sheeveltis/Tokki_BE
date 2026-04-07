using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.UseCases.Blogs.Commands.ApproveBlog;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;
using Tokki.Application.UseCases.Blogs.Commands.CreateUserBlog;
using Tokki.Application.UseCases.Blogs.Commands.DeleteBlog;
using Tokki.Application.UseCases.Blogs.Commands.IncreaseViewCount;
using Tokki.Application.UseCases.Blogs.Commands.RejectBlog;
using Tokki.Application.UseCases.Blogs.Commands.SubmitBlogForApproval;
using Tokki.Application.UseCases.Blogs.Commands.UpdateBlog;
using Tokki.Application.UseCases.Blogs.Queries.ExportBlogs;
using Tokki.Application.UseCases.Blogs.Commands.ImportBlogs;
using Tokki.Application.UseCases.Blogs.Queries;
using Tokki.Application.UseCases.Blogs.Queries.GetPagedBlogs;
using Tokki.Domain.Enums;

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

        [HttpGet("user")]
        [AllowAnonymous]
        public async Task<IActionResult> GetForUser([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, 
            [FromQuery] string? categoryId = null, [FromQuery] string? tag = null, [FromQuery] string? keyword = null)
        {
            var query = new GetPagedBlogsQuery 
            { 
                PageNumber = pageNumber, 
                PageSize = pageSize, 
                CategoryId = categoryId, 
                Tag = tag, 
                Keyword = keyword,
                Status = BlogStatus.Published 
            };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin, Staff, Moderator")]
        public async Task<IActionResult> GetForAdmin([FromQuery] GetPagedBlogsQuery query)
        {
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(string id)
        {
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff") || User.IsInRole("Moderator");
            var query = new GetBlogByIdQuery { Id = id, IsAdminView = isAdmin };
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

        [HttpPost("user")]
        [Authorize]
        public async Task<IActionResult> CreateUserBlog([FromBody] CreateUserBlogCommand command)
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

        [HttpPost("staff/submit-approval/{id}")]
        [Authorize(Roles ="Staff")] 
        public async Task<IActionResult> SubmitForApproval(string id)
        {
            var command = new SubmitBlogForApprovalCommand { BlogId = id };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("moderator/approve/{id}")]
        [Authorize(Roles = "Moderator")] 
        public async Task<IActionResult> Approve(string id)
        {
            var command = new ApproveBlogCommand { BlogId = id };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("moderator/reject")]
        [Authorize(Roles = "Moderator")] 
        public async Task<IActionResult> Reject([FromBody] RejectBlogCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("increase-view/{id}")]
        [AllowAnonymous] 
        public async Task<IActionResult> IncreaseViewCount(string id)
        {
            var command = new IncreaseViewCountCommand { BlogId = id };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("export")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Export()
        {
            var result = await _sender.Send(new ExportBlogsQuery());
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

            string fileName = $"Tokki_Blogs_{DateTime.Now:ddMMyyyy}.xlsx";
            return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost("import")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            var command = new ImportBlogsCommand { File = file };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}
