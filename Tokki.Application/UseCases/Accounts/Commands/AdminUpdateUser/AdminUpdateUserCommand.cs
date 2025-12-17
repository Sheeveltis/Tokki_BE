using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Accounts.Commands.AdminUpdateUser
{
    public class AdminUpdateUserCommand : IRequest<OperationResult<string>>
    {
        [JsonIgnore]
        public string AdminId { get; set; } = string.Empty; 

        public string TargetUserId { get; set; } = string.Empty; 
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
        public AccountRole Role { get; set; }
        public AccountStatus Status { get; set; }
    }
}
