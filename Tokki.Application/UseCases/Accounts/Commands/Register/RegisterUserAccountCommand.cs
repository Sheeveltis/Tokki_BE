using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Blogs.Commands.CreateBlog
{
    public class RegisterUserAccountCommand : IRequest<OperationResult<string>>
    {
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }

    }
}
