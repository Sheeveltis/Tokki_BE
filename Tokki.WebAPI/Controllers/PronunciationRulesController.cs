using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.UseCases.PronunciationRule.Commands.CreatePronunciationRule;
using Tokki.Application.UseCases.PronunciationRule.Commands.DeletePronunciationRule;
using Tokki.Application.UseCases.PronunciationRule.Commands.UpdatePronunciationRule;
using Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRuleById;
using Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRules;
using Tokki.Application.UseCases.Excel.Commands.ImportPronunciationRules;
using Tokki.Application.UseCases.Excel.Queries.ExportPronunciationRules;
using Tokki.Application.UseCases.Excel.Queries.GetPronunciationRuleTemplate;
using Tokki.Application.UseCases.UserPronunciation.Commands.CompletePronunciationRule;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PronunciationRulesController : ControllerBase
    {
        private readonly ISender _sender;
        public PronunciationRulesController(ISender sender)
        {
            _sender = sender;
        }
        [HttpGet]
        public async Task<IActionResult> GetPronunciationRules([FromQuery] GetPronunciationRulesQuery query)
        {
            // Tự động lấy UserId từ token nếu người dùng đã đăng nhập để check progress
            var userId = User.FindFirst("UserId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                query.UserId = userId;
            }

            var result = await _sender.Send(query);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id}/complete")]
        [Authorize]
        public async Task<IActionResult> CompleteRule(string id)
        {
            var userId = User.FindFirst("UserId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var command = new CompletePronunciationRuleCommand
            {
                UserId = userId,
                PronunciationRuleId = id
            };

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _sender.Send(new GetPronunciationRuleByIdQuery(id));
            if (!result.IsSuccess)
            {
                return result.StatusCode == 404 ? NotFound(result) : BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> CreateRule([FromBody] CreatePronunciationRuleCommand command)
        {
            var userId = User.FindFirst("UserId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            command.CreateBy = userId;
            var result = await _sender.Send(command);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateRule(string id, [FromBody] UpdatePronunciationRuleCommand command)
        {
            var userId = User.FindFirst("UserId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            command.PronunciationRuleId = id;
            command.UpdateBy = userId;
            var result = await _sender.Send(command);
            return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> DeleteRule(string id)
        {
            var result = await _sender.Send(new DeletePronunciationRuleCommand(id));
            if (!result.IsSuccess)
            {
                return result.StatusCode == 404 ? NotFound(result) : BadRequest(result);
            }
            return Ok(result);
        }
 
        [HttpPost("import-excel")]
        [Authorize(Roles = "Admin, Staff")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportByExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Vui lòng chọn file Excel.");
                }
 
                var userId = User.FindFirst("UserId")?.Value
                            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 
                var command = new ImportPronunciationRulesCommand
                {
                    File = file,
                    UserId = userId!,
                };
 
                var result = await _sender.Send(command);
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }
 
        [HttpGet("export-excel")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Export()
        {
            var result = await _sender.Send(new ExportPronunciationRulesQuery());
            if (!result.IsSuccess) return BadRequest(result);
 
            return File(result.Data.FileContent, result.Data.ContentType, result.Data.FileName);
        }

        [HttpGet("import-template")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> GetImportTemplate()
        {
            var result = await _sender.Send(new GetPronunciationRuleTemplateQuery());
            if (!result.IsSuccess) return BadRequest(result);

            return File(result.Data.FileContent, result.Data.ContentType, result.Data.FileName);
        }
    }
}
