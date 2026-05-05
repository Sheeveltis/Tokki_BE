using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.Commands.AddVocabByExcel;
using Tokki.Application.UseCases.Excel.Commands.ImportAccounts;
using Tokki.Application.UseCases.Excel.Commands.ImportQuestionsFromExcel;
using Tokki.Application.UseCases.Excel.Commands.ImportQuestionTypes;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Application.UseCases.Excel.Queries.ExportAccounts;
using Tokki.Application.UseCases.Excel.Queries.ExportQuestionTypes;
using Tokki.Application.UseCases.Excel.Queries.ExportVocabByTopic;
using Tokki.Application.UseCases.Excel.Queries.GetTemplate;
using Tokki.Application.UseCases.Excel.Queries.TemplateQuestionType;
using Tokki.Application.UseCases.Alphabet.Commands.ImportAlphabetFromExcel;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin, Staff")]
    public class ExcelController : ControllerBase
    {
        private readonly ISender _sender;
        public ExcelController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("import/vocab")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportVocabularyByExcel(IFormFile file, [FromQuery] string? topicId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Vui lòng chọn file Excel.");
                }

                var userId = User.FindFirst("UserId")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var command = new AddVocabByExcelCommand
                {
                    File = file,
                    StaffId = userId!,
                    TopicId = topicId 
                };

                var result = await _sender.Send(command);
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }
        [HttpPost("import/questions")]
        [Consumes("multipart/form-data")]
    
        public async Task<IActionResult> ImportQuestions([FromForm] ImportQuestionsFromExcelCommand command)
        {
            if (command.ExcelFile == null || command.ExcelFile.Length == 0)
            {
                var errorResult = OperationResult<ImportQuestionsResponse>.Failure(
                    new Error("File.Empty", "Vui lòng tải lên file Excel."),
                    400
                );
                return BadRequest(errorResult);
            }
            if (string.IsNullOrEmpty(command.QuestionTypeId))
            {
                var errorResult = OperationResult<ImportQuestionsResponse>.Failure(
                    new Error("Validation.MissingQuestionType", "Vui lòng nhập QuestionTypeId."),
                    400
                );
                return BadRequest(errorResult);
            }
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("import/account")]
        public async Task<IActionResult> ImportAccounts(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "Vui lòng đính kèm file Excel." });
            }

            var command = new ImportAccountCommand(file);
            var result = await _sender.Send(command);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }
        [HttpPost("import/question-types")]
        public async Task<IActionResult> ImportQuestionTypes(IFormFile file)
        {
            var result = await _sender.Send(new ImportQuestionTypesCommand(file));
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("import/alphabet")]
        public async Task<IActionResult> ImportAlphabet(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "Vui lòng đính kèm file Excel." });
            }

            var command = new ImportAlphabetFromExcelCommand(file);
            var result = await _sender.Send(command);

            return StatusCode(result.StatusCode, result);
        }
        
        [HttpGet("export/account")]
        public async Task<IActionResult> ExportAccount()
        {
            var query = new ExportAccountsQuery();
            var result = await _sender.Send(query);
            return File(result.Data.FileBytes, "application/vnd.ms-excel", result.Data.FileName);
        }
        [HttpGet("export/question-types")]
        public async Task<IActionResult> ExportQuestionTypes()
        {
            var result = await _sender.Send(new ExportQuestionTypesQuery());
            if (!result.IsSuccess) return BadRequest(result);

            return File(result.Data.FileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.Data.FileName);
        }
        [HttpGet("export/topic/{topicId}")]
        [Authorize(Roles = "Admin, Staff")] 
        public async Task<IActionResult> ExportVocabByTopic(string topicId)
        {
            var query = new ExportVocabByTopicQuery { TopicId = topicId };
            var result = await _sender.Send(query);

            if (result.IsSuccess)
            {
                return File(result.Data.FileContent, result.Data.ContentType, result.Data.FileName);
            }

            return BadRequest(result.Errors);
        }
        [HttpGet("template/account")]
        public async Task<IActionResult> GetTemplate()
        {
            var query = new GetAccountTemplateQuery();
            var result = await _sender.Send(query);

            if (!result.IsSuccess)
                return BadRequest(result);

            return File(
                result.Data.FileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.Data.FileName
            );
        }

        [HttpGet("template/question-type")]
        public async Task<IActionResult> GetTemplateQuestionType()
        {
            var query = new GetQuestionTypeTemplateQuery();
            var result = await _sender.Send(query);

            if (!result.IsSuccess)
                return BadRequest(result);

            return File(
                result.Data.FileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.Data.FileName
            );
        }
    }
}
