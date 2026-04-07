using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Blogs.Queries.ExportBlogs
{
    public class ExportBlogsQuery : IRequest<OperationResult<byte[]>>
    {
    }
}
