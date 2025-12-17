using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Accounts.Commands.AdminUpdateUser;
using Tokki.Application.UseCases.Accounts.Commands.CreateStaffAccount;
using Tokki.Application.UseCases.Accounts.Commands.FacebookLogin;
using Tokki.Application.UseCases.Accounts.Commands.GoogleLogin;
using Tokki.Application.UseCases.Accounts.Commands.Login;
using Tokki.Application.UseCases.Accounts.Commands.ResetPassword;
using Tokki.Application.UseCases.Accounts.Commands.UpdateProfile;
using Tokki.Application.UseCases.Accounts.Queries.GetAccount;
using Tokki.Application.UseCases.Accounts.Queries.GetAccountDetailById;
using Tokki.Application.UseCases.Accounts.Queries.GetUserProfile;
using Tokki.Application.UseCases.Blogs.Commands.CreateBlog;
using Tokki.Application.UseCases.Blogs.Commands.DeleteBlog;
using Tokki.Application.UseCases.Blogs.Commands.UpdateBlog;
using Tokki.Application.UseCases.Blogs.Queries;
using Tokki.Application.UseCases.Blogs.Queries.GetPagedBlogs;

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
        [HttpGet("get-all")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAllAccounts(
          [FromQuery] GetAllAccountsQuery query)
        {
           

            var result = await _sender.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết đầy đủ tài khoản theo UserId (dành cho Admin và Staff)
        /// </summary>
        [HttpGet("detail/{userId}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAccountDetailById(string userId)
        {
            var query = new GetAccountDetailByIdQuery
            {
                UserId = userId
            };

            var result = await _sender.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin(
       [FromBody] GoogleLoginCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("facebook-login")]
        public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("facebook-complete-registration")]
        public async Task<IActionResult> FacebookCompleteRegistration([FromBody] FacebookCompleteRegistrationCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _sender.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result); // Trả về 200 OK + Token
            }

            // Trả về lỗi tương ứng (400 hoặc 403)
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("register")] 
        public async Task<IActionResult> Register([FromBody] RegisterUserAccountCommand command) // ✅ Đã sửa lỗi chính tả
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("forgot-password/reset")]
        [Authorize]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            // ✅ Lấy email từ claim type đầy đủ
            var emailFromToken = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
                              ?? User.FindFirst(ClaimTypes.Email)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(emailFromToken))
            {
                return Unauthorized(new { message = "Token không chứa email." });
            }

            // Kiểm tra token type
            var tokenType = User.FindFirst("token_type")?.Value;
            if (tokenType != "reset_password")
            {
                return Unauthorized(new { message = "Token type không hợp lệ." });
            }

            // So sánh email
            if (!emailFromToken.Equals(command.Email, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Email không khớp với token." });
            }

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
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc thiếu thông tin định danh." });
            }

            var query = new GetUserProfileQuery(userId);
            var result = await _sender.Send(query);

            return StatusCode(result.StatusCode, result);
        }
        [HttpPut("update-user")]
        public async Task<IActionResult> AdminUpdate([FromBody] AdminUpdateUserCommand command)
        {
            // Lấy AdminId từ Token (Claim NameIdentifier)
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(adminId))
            {
                return Unauthorized("Không tìm thấy định danh Admin.");
            }

            command.AdminId = adminId;

            var result = await _sender.Send(command);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        [HttpPut("profile")]
        [Authorize] 
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng trong Token." });
            }

            // 2. Gán UserId vào Command (để Handler biết đang sửa ai)
            command.UserId = userId;

            // 3. Gọi Handler
            var result = await _sender.Send(command);

            return StatusCode(result.StatusCode, result);
        }
    }
}
