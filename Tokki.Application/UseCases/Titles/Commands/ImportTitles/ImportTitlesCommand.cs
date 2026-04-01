using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Titles.Commands.ImportTitles
{
    public class ImportTitlesCommand : IRequest<OperationResult<int>>
    {
        public IFormFile File { get; set; }
    }
}
