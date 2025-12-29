using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.VocabularyExample.Commands.AddExamples;
using Tokki.Application.UseCases.VocabularyExample.Commands.DeleteExample;
using Tokki.Application.UseCases.VocabularyExample.Commands.UpdateExample;
using Tokki.Application.UseCases.VocabularyExample.DTOs;
using Tokki.Application.UseCases.VocabularyExample.Queries.GetByVocabularyId;

namespace Tokki.WebAPI.Controllers
{
    [ApiController]
    [Route("api/vocabulary-examples")]
    public class VocabularyExamplesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VocabularyExamplesController(IMediator mediator)
        {
            _mediator = mediator;
        }
        // =========================
        // CREATE (POST)
        // =========================

        /// <summary>
        /// Admin: Thêm nhiều câu ví dụ vào Vocabulary
        /// </summary>
        [HttpPost("admin/add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddExamplesToVocabulary(
            [FromBody] AddVocabularyExamplesCommand command)
        {
            

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        // =========================
        // READ (GET)
        // =========================

        /// <summary>
        /// User: Lấy danh sách câu ví dụ theo VocabularyId
        /// </summary>
        [HttpGet("user/{vocabularyId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetExamplesByVocabularyId(string vocabularyId)
        {
            var query = new GetVocabularyExamplesByVocabularyIdQuery
            {
                VocabularyId = vocabularyId
            };

            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        

        // =========================
        // UPDATE (PUT)
        // =========================

        /// <summary>
        /// Admin: Cập nhật câu ví dụ theo ExampleId
        /// </summary>
        [HttpPut("admin/{exampleId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateVocabularyExample(
            string exampleId,
            [FromBody] VocabularyExampleUpdateDto updateData)
        {
            var command = new UpdateVocabularyExampleCommand
            {
                ExampleId = exampleId,
                UpdateData = updateData
            };

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // =========================
        // DELETE (DELETE)
        // =========================

        /// <summary>
        /// Admin: Xóa câu ví dụ theo ExampleId (soft delete)
        /// </summary>
        [HttpDelete("admin/{exampleId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVocabularyExample(string exampleId)
        {
            var command = new DeleteVocabularyExampleCommand
            {
                ExampleId = exampleId
            };

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}
