using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;
using Tokki.Application.UseCases.Blogs.Commands.DeleteBlog;
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBlogCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateBlogCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("ID trên URL không khớp với ID trong body.");
            }

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var command = new DeleteBlogCommand { Id = id };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}
