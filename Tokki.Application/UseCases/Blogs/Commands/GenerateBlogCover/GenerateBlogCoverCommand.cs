using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Blogs.Commands.GenerateBlogCover
{
    public class GenerateBlogCoverCommand : IRequest<OperationResult<byte[]>>
    {
        public string Title { get; set; } = string.Empty;
    }
}
