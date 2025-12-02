using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Categories.Commands.CreateCategory;
using Tokki.Application.UseCases.Categories.Commands.DeleteCategory;
using Tokki.Application.UseCases.Categories.Commands.UpdateCategory;
using Tokki.Application.UseCases.Categories.Queries;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ISender _sender;

        public CategoryController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _sender.Send(new GetAllCategoriesQuery());
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateCategoryCommand command)
        {
            if (id != command.Id)
                return BadRequest("ID in URL does not match ID in body.");

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var command = new DeleteCategoryCommand { Id = id };

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}
