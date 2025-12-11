using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Topics.Commands.CreateTopic;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopic;
using Tokki.Application.UseCases.Topics.Commands.DeleteTopic;
using Tokki.Application.UseCases.Topics.Queries.GetById;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Application.UseCases.Topics.Queries;
using Tokki.Domain.Enums;

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


        [HttpGet]
        public async Task<IActionResult> GetAllTopics(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] TopicStatus? status = null)
        {
            var query = new GetAllTopicsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Status = status
            };

            var result = await _mediator.Send(query);

            return StatusCode(result.StatusCode, result);
        }


        [HttpGet("{topicId}")]
        public async Task<IActionResult> GetTopicById(string topicId)
        {
            var query = new GetTopicByIdQuery
            {
                TopicId = topicId
            };

            var result = await _mediator.Send(query);

            return StatusCode(result.StatusCode, result);
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

        [HttpPut("Update")]
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
    }

    #region Request Models



    #endregion
}