using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Passages.Commands.CreatePassage;
using Tokki.Application.UseCases.Passages.Commands.DeletePassage;
using Tokki.Application.UseCases.Passages.Commands.UpdatePassage;
using Tokki.Application.UseCases.Passages.Queries.GetPassageById;
using Tokki.Application.UseCases.Passages.Queries.GetPassages;

namespace Tokki.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PassagesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PassagesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo mới đoạn văn
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePassageCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật đoạn văn
        /// </summary>
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdatePassageCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xóa đoạn văn (tùy handler: xóa cứng hoặc ẩn)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeletePassageCommand { PassageId = id });
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy chi tiết đoạn văn theo Id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetPassageByIdQuery { PassageId = id });
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách đoạn văn (mặc định lấy tất cả; nếu truyền status thì filter theo status)
        /// </summary>
        /// <remarks>
        /// Ví dụ:
        /// - GET /api/passages?pageNumber=1&pageSize=10
        /// - GET /api/passages?status=Active&pageNumber=1&pageSize=10
        /// - GET /api/passages?status=Hidden&pageNumber=1&pageSize=10
        /// - GET /api/passages?searchTerm=abc&mediaType=Text&pageNumber=1&pageSize=10
        /// </remarks>
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] GetPassagesQuery query)
        {
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }
    }
}
