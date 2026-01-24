using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Exam.Commands.AddQuestionToExam;
using Tokki.Application.UseCases.Exam.Commands.CreateExam;
using Tokki.Application.UseCases.Exam.Commands.DeleteExam;
using Tokki.Application.UseCases.Exam.Commands.RemoveQuestionFromExam;
using Tokki.Application.UseCases.Exam.Commands.UpdateExam;
using Tokki.Application.UseCases.Exam.Queries.GetExamById;
using Tokki.Application.UseCases.Exam.Queries.GetExamDetailQuery;
using Tokki.Application.UseCases.Exam.Queries.GetExams;

namespace Tokki.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExamsController : ControllerBase
    {
        private readonly ISender _sender;

        public ExamsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateExam([FromBody] CreateExamCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("admin")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAllExamsForAdmin([FromQuery] GetExamsQuery query )
        {
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("admin/detail")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetExamDetail(string id)
        {
            var query = new GetExamDetailQuery { ExamId = id };

            var result = await _sender.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
    }
}
