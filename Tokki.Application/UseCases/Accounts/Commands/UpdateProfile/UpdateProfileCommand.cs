using System.Text.Json.Serialization;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Accounts.Commands.UpdateProfile
{
    public class UpdateProfileCommand : IRequest<OperationResult<string>>
    {
        [JsonIgnore]
        public string? UserId { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
    }
}