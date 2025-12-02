using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Blogs.DTOs;

namespace Tokki.Application.UseCases.Blogs.Queries
{
    public class GetBlogByIdQuery : IRequest<OperationResult<BlogDetailDTO>>
    {
        public string Id { get; set; } = string.Empty;
    }
}
