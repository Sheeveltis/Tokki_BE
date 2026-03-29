using FluentValidation;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using System.Text.Json; // Added for JSON serialization

namespace Tokki.Application.UseCases.Otps.Commands.SendGeneralOtp
{
    public class SendGeneralOtpCommandHandler
        : IRequestHandler<SendGeneralOtpCommand, OperationResult<string>>
    {
        private readonly IRedisService _redisService;
        private readonly IEmailService _emailService;
        private readonly IAccountRepository _accountRepository;

        public SendGeneralOtpCommandHandler(
            IRedisService redisService,
            IEmailService emailService,
            IAccountRepository accountRepository)
        {
            _redisService = redisService;
            _emailService = emailService;
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<string>> Handle(
            SendGeneralOtpCommand request,
            CancellationToken cancellationToken)
        {
            // Kiểm tra email tồn tại trong CSDL
            var user = await _accountRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.UserNotFound });
            }

            // Tạo mã OTP ngẫu nhiên
            var otpCode = new Random().Next(100000, 999999).ToString();

            // Lưu vào Redis
            var redisKey = $"OTP:General:{request.Email}";
            var redisValue = JsonSerializer.Serialize(new { OtpCode = otpCode, AttemptCount = 0 });
            await _redisService.SetAsync(redisKey, redisValue, TimeSpan.FromMinutes(5));

            // Gửi email 
            string subject = "Mã xác thực ";
            string body =
                $"<h3>Mã xác thực của bạn là: " +
                $"<b style='color:blue; font-size:20px;'>{otpCode}</b></h3>" +
                $"<p>Mã có hiệu lực trong 5 phút.</p>";

            await _emailService.SendEmailAsync(request.Email, subject, body);

            return OperationResult<string>.Success("Đã gửi OTP thành công.", 200);
        }
    }
}