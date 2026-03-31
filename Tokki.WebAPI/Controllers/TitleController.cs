using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.UseCases.Titles.Commands.CreateTitle;
using Tokki.Application.UseCases.Titles.Commands.DeleteTitle;
using Tokki.Application.UseCases.Titles.Commands.UpdateTitle;
using Tokki.Application.UseCases.Titles.Commands.ImportTitles;
using Tokki.Application.UseCases.Titles.Commands.EquipTitle;
using Tokki.Application.UseCases.Titles.Queries.GetAllTitles;
using Tokki.Application.UseCases.Titles.Queries.GetPagedTitles;
using Tokki.Application.UseCases.Titles.Queries.GetTitleById;
using Tokki.Application.UseCases.Titles.Queries.ExportTitles;
using Tokki.Application.UseCases.Titles.Queries.GetUnlockedTitles;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class TitleController : ControllerBase
    {
        private readonly ISender _sender;

        public TitleController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Create([FromBody] CreateTitleCommand command)
        {
            var result = await _sender.Send(command);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return StatusCode(201, result);
        }

        [HttpGet("admin")]
        public async Task<IActionResult> GetPaged([FromQuery] GetPagedTitlesQuery query)
        {
            var result = await _sender.Send(query);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _sender.Send(new GetTitleByIdQuery(id));
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTitleCommand command)
        {
            if (id != command.TitleId) command.TitleId = id;
            var result = await _sender.Send(command);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _sender.Send(new DeleteTitleCommand(id));
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        [HttpPost("import")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            var result = await _sender.Send(new ImportTitlesCommand { File = file });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("export")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Export()
        {
            var result = await _sender.Send(new ExportTitlesQuery());
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            string fileName = $"Tokki_Title_{DateTime.Now:ddMMyyyy}.xlsx";
            return File(result.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // --- NEW USER APIS ---

        [HttpPut("equip")]
        public async Task<IActionResult> Equip([FromBody] EquipTitleCommand command)
        {
            if (string.IsNullOrEmpty(command.UserId)) command.UserId = GetUserId()!;
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("my-titles")]
        public async Task<IActionResult> GetMyTitles([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _sender.Send(new GetUnlockedTitlesQuery 
            { 
                UserId = GetUserId()!,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("user/check-level-titles")]
        public async Task<IActionResult> CheckLevelTitles()
        {
            var result = await _sender.Send(new Tokki.Application.UseCases.Titles.Commands.CheckLevelTitles.CheckLevelTitlesCommand { UserId = GetUserId()! });
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("user/check-daily-titles")]
        public async Task<IActionResult> CheckDailyTitles()
        {
            var result = await _sender.Send(new Tokki.Application.UseCases.Titles.Commands.CheckDailyTitles.CheckDailyTitlesCommand { UserId = GetUserId()! });
            return StatusCode(result.StatusCode, result);
        }

        private string? GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? User.FindFirstValue("userId");
        }
    }
}