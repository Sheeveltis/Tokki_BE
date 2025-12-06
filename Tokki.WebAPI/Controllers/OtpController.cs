using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Accounts.Commands.SendEmailVerificationOtp;
using Tokki.Application.UseCases.Otps.Commands.SendOtpForEmailVerification;

namespace Tokki.API.Controllers
{
    [Route("api/[controller]")] // Đường dẫn sẽ là: api/otp
    [ApiController]
    public class OtpController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OtpController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Gửi mã OTP xác thực đăng nhập qua Email
        /// </summary>
        /// <param name="command">Chứa email người dùng gửi lên</param>
        /// <returns>Kết quả gửi thành công hoặc thông báo lỗi</returns>
        [HttpPost("send-login-verification")]
        public async Task<IActionResult> SendLoginVerificationOtp([FromBody] SendEmailVerificationOtpCommand command)
        {
            // Gọi sang Handler thông qua MediatR
            var result = await _mediator.Send(command);

            // Kiểm tra kết quả trả về từ OperationResult
            if (result.IsSuccess)
            {
                // Trả về 200 OK kèm message
                return Ok(result);
            }

            // Trả về lỗi (400, 403, 404, 500...) tùy theo StatusCode trong Result
            return StatusCode(result.StatusCode, result);
        }
    }
}