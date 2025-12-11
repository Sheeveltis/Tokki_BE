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

        /// <summary>
        /// Lấy danh sách chủ đề (có phân trang và tìm kiếm)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(OperationResult<PagedResult<TopicDto>>), 200)]
        //public async Task<IActionResult> GetAllTopics(
        //    [FromQuery] int pageNumber = 1,
        //    [FromQuery] int pageSize = 10,
        //    [FromQuery] string? searchTerm = null,
        //    [FromQuery] bool? isActive = null)
        //{
        //    var query = new GetAllTopicsQuery
        //    {
        //        PageNumber = pageNumber,
        //        PageSize = pageSize,
        //        SearchTerm = searchTerm,
        //        IsActive = isActive
        //    };

        //    var result = await _mediator.Send(query);

        //    return StatusCode(result.StatusCode, result);
        //}

        /// <summary>
        /// Lấy chi tiết một chủ đề theo ID
        /// </summary>
        [HttpGet("{topicId}")]
        [ProducesResponseType(typeof(OperationResult<TopicDto>), 200)]
        [ProducesResponseType(typeof(OperationResult<TopicDto>), 404)]
        public async Task<IActionResult> GetTopicById(string topicId)
        {
            var query = new GetTopicByIdQuery
            {
                TopicId = topicId
            };

            var result = await _mediator.Send(query);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo chủ đề mới
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(OperationResult<string>), 201)]
        [ProducesResponseType(typeof(OperationResult<string>), 400)]
        [ProducesResponseType(typeof(OperationResult<string>), 409)]
        public async Task<IActionResult> CreateTopic([FromBody] CreateTopicRequest request)
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

            var command = new CreateTopicCommand
            {
                TopicName = request.TopicName,
                Description = request.Description,
                CreateBy = userId
            };

            var result = await _mediator.Send(command);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật thông tin chủ đề
        /// </summary>
        [HttpPut("{topicId}")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 400)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        [ProducesResponseType(typeof(OperationResult<bool>), 409)]
        public async Task<IActionResult> UpdateTopic(string topicId, [FromBody] UpdateTopicRequest request)
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

            var command = new UpdateTopicCommand
            {
                TopicId = topicId,
                TopicName = request.TopicName,
                Description = request.Description,
                UpdatedBy = userId,
                IsActive = request.IsActive
            };

            var result = await _mediator.Send(command);

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xóa chủ đề
        /// </summary>
        [HttpDelete("{topicId}")]
        [ProducesResponseType(typeof(OperationResult<bool>), 200)]
        [ProducesResponseType(typeof(OperationResult<bool>), 404)]
        [ProducesResponseType(typeof(OperationResult<bool>), 409)]
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

    /// <summary>
    /// Request model cho việc tạo chủ đề mới
    /// </summary>
    public class CreateTopicRequest
    {
        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Request model cho việc cập nhật chủ đề
    /// </summary>
    public class UpdateTopicRequest
    {
        public string TopicName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion
}