using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Blogs.Commands.RejectBlog
{
    public class RejectBlogCommand : IRequest<OperationResult<bool>>
    {
        public string BlogId { get; set; } = string.Empty;
        public string RejectReason { get; set; } = string.Empty;
    }
}
