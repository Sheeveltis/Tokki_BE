using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Topics.Commands.CreateTopic;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopic;
using Tokki.Application.UseCases.Topics.Commands.DeleteTopic;
using Tokki.Application.UseCases.Topics.Queries.GetById;
using Tokki.Application.UseCases.Topics.Queries;
using Tokki.Domain.Enums;
using Tokki.Application.UseCases.Topics.Commands.AddVocabulariesToTopic;
using Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic;
using Tokki.Application.UseCases.Topics.Commands.PublishTopic;

namespace Tokki.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TopicsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TopicsController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpGet("admin/get-all")]
        public async Task<IActionResult> GetAllTopics(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] TopicStatus? status = null,
            [FromQuery] TopicLevel? level = null)
        {
            var query = new GetAllTopicsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Level = level,
                Status = status
            };

            var result = await _mediator.Send(query);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("user/get-all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTopicsForUser(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] TopicLevel? level = null)
        {
            var query = new GetAllTopicsForUserQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Level = level
            };

            var result = await _mediator.Send(query);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{topicId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTopicById(string topicId)
        {
            var query = new GetTopicDetailByIdQuery
            {
                TopicId = topicId
            };

            var result = await _mediator.Send(query);

            return StatusCode(result.StatusCode, result);
        }

        // Sửa lại URL template, bỏ {topicId} ra khỏi path
        [HttpPost("vocabularies")]
        [Authorize]
        // Nhận toàn bộ Command từ Body. ASP.NET Core sẽ tự động gán dữ liệu.
        public async Task<IActionResult> AddVocabulariesToTopic([FromBody] AddVocabulariesToTopicCommand command)
        {
            
            // Kiểm tra Command có null không (tùy chọn)
            if (command == null)
            {
                return BadRequest("Dữ liệu không được để trống.");
            }

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> CreateTopic([FromBody] CreateTopicCommand request)
        {
            // Lấy UserId từ JWT token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return StatusCode(401, OperationResult<string>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                ));
            }

            var result = await _mediator.Send(request);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateTopic([FromBody] UpdateTopicCommand command)
        {
            // Lấy UserId từ JWT token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return StatusCode(401, OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                ));
            }
            var result = await _mediator.Send(command);

            return StatusCode(result.StatusCode, result);
        }
        [HttpPut("{topicId}/publish")]
        public async Task<IActionResult> Publish(string topicId)
        {
            var result = await _mediator.Send(new PublishTopicCommand { TopicId = topicId });
            return StatusCode(result.StatusCode, result);
        }


        [HttpDelete("{topicId}")]
        public async Task<IActionResult> DeleteTopic(string topicId)
        {
            var command = new DeleteTopicCommand
            {
                TopicId = topicId
            };

            var result = await _mediator.Send(command);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("admin/vocabularies")]
        [Authorize]
        public async Task<IActionResult> RemoveVocabulariesFromTopic(
    [FromBody] RemoveVocabulariesFromTopicCommand command)
        {
            if (command == null)
            {
                return BadRequest("Dữ liệu không được để trống.");
            }

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
    }

    #region Request Models



    #endregion
}