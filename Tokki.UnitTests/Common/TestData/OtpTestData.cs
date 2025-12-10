using System.Collections.Generic;
using Tokki.Application.UseCases.Otps.Commands.ForgotPassword;
using Tokki.Application.UseCases.Otps.Commands.SendGeneralOtp;
using Tokki.Application.UseCases.Otps.Commands.SendOtpForEmailVerification;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    public static class OtpTestData
    {
        public static SendForgotPasswordOtpCommand GetValidForgotPasswordCommand()
        {
            return new SendForgotPasswordOtpCommand
            {
                Email = "user@example.com"
            };
        }

        public static Account GetActiveAccount()
        {
            return new Account
            {
                UserId = "acc-1",
                Email = "user@example.com",
                FullName = "Test User",
                Status = AccountStatus.Active // Quan trọng: Phải Active
            };
        }

        public static Account GetBannedAccount()
        {
            return new Account
            {
                UserId = "acc-banned",
                Email = "banned@example.com",
                Status = AccountStatus.Banned // Quan trọng: Banned
            };
        }
        public static SendGeneralOtpCommand GetValidGeneralOtpCommand()
        {
            return new SendGeneralOtpCommand
            {
                Email = "general-user@example.com"
            };
        }

        public static SendEmailVerificationOtpCommand GetEmailVerificationCommand()
        {
            return new SendEmailVerificationOtpCommand
            {
                Email = "verify@example.com"
            };
        }

        public static Otp GetRecentOtp(string email)
        {
            return new Otp
            {
                OtpId = "otp-recent",
                Email = email,
                Type = OtpType.VerifyEmail,
                CreatedAt = DateTime.UtcNow.AddHours(7).AddSeconds(-10), // Tạo cách đây 10s
                ExpiredAt = DateTime.UtcNow.AddHours(7).AddMinutes(5),
                Status = OtpStatus.Active
            };
        }
    }
}