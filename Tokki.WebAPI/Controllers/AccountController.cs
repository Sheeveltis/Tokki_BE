using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Accounts.Commands.Login;
using Tokki.Application.UseCases.Accounts.Commands.ResetPassword;
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
    }
}
