using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Blogs.Commands.IncreaseViewCount
{
    public class IncreaseViewCountCommand : IRequest<OperationResult<bool>>
    {
        public string BlogId { get; set; } = string.Empty;
    }
}
