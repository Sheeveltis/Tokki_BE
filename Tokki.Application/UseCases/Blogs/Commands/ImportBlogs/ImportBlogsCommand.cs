using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Blogs.Commands.ImportBlogs
{
    public class ImportBlogsCommand : IRequest<OperationResult<bool>>
    {
        public IFormFile File { get; set; } = default!;
    }
}
