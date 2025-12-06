using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Otps.Commands.SendOtpForEmailVerification
{
    public class SendEmailVerificationOtpCommand : IRequest<OperationResult<string>>
    {
        public string Email { get; set; }
    }
}
