using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Topics.Commands.AddVocabulariesToTopic;
using Tokki.Application.UseCases.Topics.Commands.ApproveTopic;
using Tokki.Application.UseCases.Topics.Commands.CreateTopic;
using Tokki.Application.UseCases.Topics.Commands.CreateTopicByStaff;
using Tokki.Application.UseCases.Topics.Commands.DeleteTopic;
using Tokki.Application.UseCases.Topics.Commands.PublishTopic;
using Tokki.Application.UseCases.Topics.Commands.RejectTopic;
using Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic;
using Tokki.Application.UseCases.Topics.Commands.SubmitTopicForApproval;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopic;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopicOrderIndex;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopicStatus;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Application.UseCases.Topics.Queries;
using Tokki.Application.UseCases.Topics.Queries.CheckTopicCompletion;
using Tokki.Application.UseCases.Topics.Queries.GetById;
using Tokki.Application.UseCases.Topics.Queries.GetStudyVocabs;
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
            var userId = User.FindFirst("UserId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            var query = new GetAllTopicsForUserQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Level = level,
                UserId = userId
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
        /// <summary>
        /// Kho - api dùng cho việc lấy danh sách từ vựng chưa học của user trong 1 topic  
        /// </summary>
        /// <param name="topicId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [HttpGet("user/study")]
        [Authorize]
        public async Task<IActionResult> GetStudyVocabs(
        [FromQuery] string topicId,
        [FromQuery] int count = 10)
        {
            var userId = User.FindFirst("UserId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            var query = new GetStudyVocabsQuery
            {
                UserId = userId,
                TopicId = topicId,
                Count = count
            };

            var result = await _mediator.Send(query);
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        /// <summary>
        /// Kho - kiểm tra xem user đã hoàn thành topic chưa
        /// </summary>
        /// <param name="topicId"></param>
        /// <returns></returns>
        [HttpGet("user/completion-status")]
        [Authorize]
        public async Task<IActionResult> GetTopicCompletionStatus([FromQuery] string topicId)
        {
            var userId = User.FindFirst("UserId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            var query = new CheckTopicCompletionQuery
            {
                TopicId = topicId,
                UserId = userId
            };
            var result = await _mediator.Send(query);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
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
        [HttpPost("staff/create-topic")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> CreateTopicByStaff(
    [FromBody] CreateTopicByStaffCommand command)
        {
            var result = await _mediator.Send(command);
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

        [HttpPost("staff/submit-for-approval/{topicId}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> SubmitTopicForApproval(string topicId)
        {
            var result = await _mediator.Send(new SubmitTopicForApprovalCommand
            {
                TopicId = topicId
            });

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("moderator/approve-topic/{topicId}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> ApproveTopic(string topicId)
        {
            var result = await _mediator.Send(new ApproveTopicCommand
            {
                TopicId = topicId
            });

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("moderator/reject-topic/{topicId}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> RejectTopic(
        string topicId,
     [FromBody] RejectTopicRequest request)
        {
            var result = await _mediator.Send(new RejectTopicCommand
            {
                TopicId = topicId,
                RejectReason = request.RejectReason
            });

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
            command.UpdatedBy = userId;

            var result = await _mediator.Send(command);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateTopicStatus([FromBody] UpdateTopicStatusCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return StatusCode(401, OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                ));
            }

            command.UpdatedBy = userId;

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPut("update-order-index")]
        public async Task<IActionResult> UpdateTopicOrderIndex([FromBody] UpdateTopicOrderIndexCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return StatusCode(401, OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                ));
            }

            command.UpdatedBy = userId;

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