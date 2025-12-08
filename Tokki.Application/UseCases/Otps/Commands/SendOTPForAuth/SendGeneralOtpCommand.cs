using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Otps.Commands.SendGeneralOtp
{
    public class SendGeneralOtpCommand : IRequest<OperationResult<string>>
    {
        public string Email { get; set; } = string.Empty;
    }
}