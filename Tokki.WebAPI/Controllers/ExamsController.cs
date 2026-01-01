using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Exam.Commands.AddQuestionToExam;
using Tokki.Application.UseCases.Exam.Commands.CreateExam;
using Tokki.Application.UseCases.Exam.Commands.DeleteExam;
using Tokki.Application.UseCases.Exam.Commands.RemoveQuestionFromExam;
using Tokki.Application.UseCases.Exam.Commands.UpdateExam;
using Tokki.Application.UseCases.Exam.Queries.GetExamById;
using Tokki.Application.UseCases.Exam.Queries.GetExams;

namespace Tokki.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExamsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ExamsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // =========================
        // CREATE (POST)
        // =========================

        /// <summary>
        /// Tạo bài test mới từ mẫu đề thi
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateExam([FromBody] CreateExamCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Thêm câu hỏi vào bài test
        /// </summary>
        [HttpPost("{id}/questions")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddQuestionToExam(string id, [FromBody] AddQuestionToExamCommand command)
        {
            if (id != command.ExamId)
            {
                return BadRequest(new { message = "ID không khớp" });
            }

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // =========================
        // READ (GET)
        // =========================

        /// <summary>
        /// Lấy danh sách bài test (phân trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetExams([FromQuery] GetExamsQuery query)
        {
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy chi tiết bài test theo ID (kèm danh sách câu hỏi)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExamById(string id)
        {
            var query = new GetExamByIdQuery { ExamId = id };
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        // =========================
        // UPDATE (PUT)
        // =========================

        /// <summary>
        /// Cập nhật thông tin bài test
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateExam(string id, [FromBody] UpdateExamCommand command)
        {
            if (id != command.ExamId)
            {
                return BadRequest(new { message = "ID không khớp" });
            }

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // =========================
        // DELETE (DELETE)
        // =========================

        /// <summary>
        /// Xóa bài test
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteExam(string id)
        {
            var command = new DeleteExamCommand { ExamId = id };
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xóa câu hỏi khỏi bài test
        /// </summary>
        [HttpDelete("{id}/questions/{questionNo}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveQuestionFromExam(string id, int questionNo)
        {
            var command = new RemoveQuestionFromExamCommand
            {
                ExamId = id,
                QuestionNo = questionNo
            };

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}
