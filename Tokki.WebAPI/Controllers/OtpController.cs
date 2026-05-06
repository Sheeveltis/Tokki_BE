using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Accounts.Commands.SendEmailVerificationOtp;
using Tokki.Application.UseCases.Accounts.Commands.VerifyEmailOtp;
using Tokki.Application.UseCases.Otps.Commands.ForgotPassword;
using Tokki.Application.UseCases.Otps.Commands.SendGeneralOtp;
using Tokki.Application.UseCases.Otps.Commands.SendOtpForEmailVerification;
using Tokki.Application.UseCases.Otps.Commands.VerifyForgotPasswordOtp;

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
        [HttpPost("send-otp-for-email-verification")]
        public async Task<IActionResult> SendOTPForEmailVerificationOtp([FromBody] SendEmailVerificationOtpCommand command)
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
        // POST: api/otp/send-general
        [HttpPost("send-general")]
        public async Task<IActionResult> SendGeneralOtp([FromBody] SendGeneralOtpCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        [HttpPost("verify-login-otp")]
        public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyEmailOtpCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("forgot-password/send-otp")]
        public async Task<IActionResult> SendForgotOtp([FromBody] SendForgotPasswordOtpCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("forgot-password/verify")]
        public async Task<IActionResult> VerifyForgotOtp([FromBody] VerifyForgotPasswordOtpCommand command)
        {
            var result = await _mediator.Send(command);
            // Result.Data ở đây chính là cái ResetToken (string)
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("test-smtp")]
        public async Task<IActionResult> TestSmtpConnection()
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                var connectTask = client.ConnectAsync("smtp.gmail.com", 587);
                
                // Set timeout là 15 giây
                if (await Task.WhenAny(connectTask, Task.Delay(15000)) == connectTask)
                {
                    // Lệnh kết nối hoàn thành trong 15s (có thể thành công hoặc văng lỗi)
                    await connectTask; 
                    return Ok(new 
                    { 
                        success = true, 
                        message = "Tuyệt vời! Kết nối tới smtp.gmail.com:587 THÀNH CÔNG. Port này KHÔNG BỊ CHẶN." 
                    });
                }
                else
                {
                    // Hết 15s mà vẫn chưa kết nối được
                    return StatusCode(504, new 
                    { 
                        success = false, 
                        message = "Timeout! Không thể kết nối tới smtp.gmail.com:587 sau 15 giây. 99% Cổng 587 ĐÃ BỊ NHÀ MẠNG VPS CHẶN.",
                        recommendation = "Hãy liên hệ bộ phận hỗ trợ của VPS yêu cầu họ mở khóa (unblock) outbound port 587 cho SMTP."
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "Lỗi khi cố gắng kết nối tới smtp.gmail.com:587. Port có thể bị chặn cứng (Connection Refused).", 
                    error = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }
    }
}