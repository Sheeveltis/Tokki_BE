using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Otps.Commands.VerifyForgotPasswordOtp
{
    public class VerifyForgotPasswordOtpCommand : IRequest<OperationResult<string>>
    {
        public string Email { get; set; }
        public string OtpCode { get; set; }
    }
}
