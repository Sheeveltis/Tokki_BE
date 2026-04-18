using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.Queries.GetPronunciationExampleTemplate;
using Tokki.Application.UseCases.PronunciationExample.Commands.CreatePronunciationExample;
using Tokki.Application.UseCases.PronunciationExample.Commands.DeletePronunciationExample;
using Tokki.Application.UseCases.PronunciationExample.Commands.UpdatePronunciationExample;
using Tokki.Application.UseCases.PronunciationExample.Queries.GetExampleDetail;
using Tokki.Application.UseCases.PronunciationExample.Queries.GetExamplesByRuleId;
using Tokki.Application.UseCases.PronunciationExample.Queries.GetPagedPronunciationExamples;
using Tokki.Application.UseCases.Excel.Commands.ImportPronunciationExample;
using Tokki.Application.UseCases.Excel.Queries.ExportPronunciationExamples;
using Tokki.Application.UseCases.UserPronunciation.Commands.PracticePronunciationExample;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PronunciationExampleController : ControllerBase
    {
        private readonly ISender _sender;

        public PronunciationExampleController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("{id}/practice")]
        [Authorize]
        public async Task<IActionResult> Practice(string id)
        {
            var userId = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _sender.Send(new PracticePronunciationExampleCommand
            {
                UserId = userId,
                PronunciationExampleId = id
            });
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] GetPagedPronunciationExamplesQuery query)
        {
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _sender.Send(new GetExampleDetailQuery(id));
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("rules/{ruleId}/examples")]
        public async Task<IActionResult> GetByRuleId(string ruleId)
        {
            var result = await _sender.Send(new GetExamplesByRuleIdQuery(ruleId));
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Create([FromBody] CreatePronunciationExampleCommand command)
        {
            command.UserId = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePronunciationExampleCommand command)
        {
            command.ExampleId = id;
            command.UserId = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _sender.Send(new DeletePronunciationExampleCommand { ExampleId = id });
            return StatusCode(result.StatusCode, result);
        }
 
        [HttpPost("import-excel")]
        [Authorize(Roles = "Admin, Staff")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportByExcel([FromForm] ImportPronunciationExampleRequest request)
        {
            try
            {
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest("Vui lòng chọn file Excel.");
                }

                if (string.IsNullOrEmpty(request.RuleId))
                {
                    return BadRequest("Vui lòng cung cấp PronunciationRuleId.");
                }
 
                var userId = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
 
                var command = new ImportPronunciationExampleCommand
                {
                    File = request.File,
                    PronunciationRuleId = request.RuleId,
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

        public class ImportPronunciationExampleRequest
        {
            public IFormFile File { get; set; } = null!;
            public string RuleId { get; set; } = string.Empty;
        }
 
        [HttpGet("export-excel")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> Export([FromQuery] string? ruleId)
        {
            var result = await _sender.Send(new ExportPronunciationExamplesQuery { PronunciationRuleId = ruleId });
            if (!result.IsSuccess) return BadRequest(result);
 
            return File(result.Data.FileContent, result.Data.ContentType, result.Data.FileName);
        }

        [HttpGet("import-template")]
        [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> GetImportTemplate()
        {
            var result = await _sender.Send(new GetPronunciationExampleTemplateQuery());
            if (!result.IsSuccess) return BadRequest(result);

            return File(result.Data.FileContent, result.Data.ContentType, result.Data.FileName);
        }
    }
}
