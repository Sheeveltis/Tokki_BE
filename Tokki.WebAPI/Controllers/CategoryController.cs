using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Categories.Commands.CreateCategory;
using Tokki.Application.UseCases.Categories.Commands.DeleteCategory;
using Tokki.Application.UseCases.Categories.Commands.UpdateCategory;
using Tokki.Application.UseCases.Categories.Queries;
using Tokki.Application.UseCases.Categories.Queries.GetCategoryById;
using Tokki.Application.UseCases.Categories.Queries.GetPagedCategories;
using Tokki.Application.UseCases.Categories.Commands.ImportCategories;
using Tokki.Application.UseCases.Categories.Queries.ExportCategories;

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

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] GetPagedCategoriesQuery query)
        {
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _sender.Send(new GetCategoryByIdQuery(id));
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateCategoryCommand command)
        {
            if (id != command.Id)
                return BadRequest("ID in URL does not match ID in body.");

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Delete(string id)
        {
            var command = new DeleteCategoryCommand { Id = id };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("export")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Export()
        {
            var result = await _sender.Send(new ExportCategoriesQuery());
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);

            string fileName = $"Tokki_Category_{DateTime.Now:ddMMyyyy}.xlsx";
            return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost("import")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            var command = new ImportCategoriesCommand { File = file };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}
