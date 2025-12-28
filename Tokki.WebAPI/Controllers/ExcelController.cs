using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech.Transcription;
using System.Security.Claims;
using Tokki.Application.UseCases.Excel.Commands.AddVocabByExcel;
using Tokki.Application.UseCases.LiveChat.Commands.CreateSupportChat;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExcelController : ControllerBase
    {

        private readonly ISender _sender;
        public ExcelController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("add-vocab")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin, Staff")]
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
    }
}
