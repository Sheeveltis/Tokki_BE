using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.Commands.AdminSoftDeleteAccount;
using Tokki.Application.UseCases.Accounts.Commands.AdminUpdateUser;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount;
using Tokki.Application.UseCases.Accounts.Commands.DeleteAccount;
using Tokki.Application.UseCases.Accounts.Commands.FacebookLogin;
using Tokki.Application.UseCases.Accounts.Commands.GoogleLogin;
using Tokki.Application.UseCases.Accounts.Commands.Login;
using Tokki.Application.UseCases.Accounts.Commands.ResetPassword;
using Tokki.Application.UseCases.Accounts.Commands.UpdateMyLevel;
using Tokki.Application.UseCases.Accounts.Commands.UpdateProfile;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Accounts.Queries.GetAccount;
using Tokki.Application.UseCases.Accounts.Queries.GetAccountDetailById;
using Tokki.Application.UseCases.Accounts.Queries.GetMyLevel;
using Tokki.Application.UseCases.Accounts.Queries.GetUserProfile;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ISender _sender;

        public AccountController(ISender sender)
        {
            _sender = sender;
        }

        // =========================================================
        // CREATE
        // =========================================================

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("facebook-login")]
        [AllowAnonymous]
        public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("facebook-complete-registration")]
        [AllowAnonymous]
        public async Task<IActionResult> FacebookCompleteRegistration([FromBody] FacebookCompleteRegistrationCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterUserAccountCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("create-account")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountByAdminCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("forgot-password/reset")]
        [Authorize]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            var emailFromToken =
                User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(emailFromToken))
                return Unauthorized(new { message = "Token không chứa email." });

            var tokenType = User.FindFirst("token_type")?.Value;
            if (tokenType != "reset_password")
                return Unauthorized(new { message = "Token type không hợp lệ." });

            if (!emailFromToken.Equals(command.Email, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Email không khớp với token." });

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // =========================================================
        // GET
        // =========================================================

        [HttpGet("get-all")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAllAccounts([FromQuery] GetAllAccountsQuery query)
        {
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết đầy đủ tài khoản theo UserId (Admin/Staff)
        /// </summary>
        [HttpGet("detail/{userId}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAccountDetailById(string userId)
        {
            var query = new GetAccountDetailByIdQuery { UserId = userId };
            var result = await _sender.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Token không hợp lệ hoặc thiếu thông tin định danh." });

            var query = new GetUserProfileQuery(userId);
            var result = await _sender.Send(query);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("me/level")]
        [Authorize]
        public async Task<IActionResult> GetMyLevel(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(OperationResult<GetMyLevelResponse>.Failure("Unauthorized", 401));

            var query = new GetMyLevelQuery { UserId = userId };
            var result = await _sender.Send(query, cancellationToken);

            return StatusCode(result.StatusCode, result);
        }

        // =========================================================
        // UPDATE
        // =========================================================

        [HttpPut("update-user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUpdate([FromBody] AdminUpdateUserCommand command)
        {
            var adminId = GetUserId();
            if (string.IsNullOrEmpty(adminId))
                return Unauthorized("Không tìm thấy định danh Admin.");

            command.AdminId = adminId;

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng trong Token." });

            command.UserId = userId;

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("me/level")]
        [Authorize]
        public async Task<IActionResult> UpdateMyLevel([FromBody] UpdateMyLevelCommand command, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(OperationResult<bool>.Failure("Unauthorized", 401));

            command.UserId = userId;

            var result = await _sender.Send(command, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        // =========================================================
        // DELETE
        // =========================================================

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng trong Token." });

            var command = new DeleteAccountCommand { UserId = userId };
            var result = await _sender.Send(command);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDeleteUserAccount([FromRoute] string userId)
        {
            var adminId = GetUserId();
            if (string.IsNullOrEmpty(adminId))
                return Unauthorized(new { message = "Không tìm thấy định danh Admin." });

            var command = new AdminSoftDeleteAccountCommand
            {
                TargetUserId = userId,
                AdminUserId = adminId
            };

            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        // =========================================================
        // Helpers
        // =========================================================

        private string? GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? User.FindFirstValue("userId");
        }
    }
}
