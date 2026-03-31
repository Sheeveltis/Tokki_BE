using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using System.Collections.Generic;

namespace Tokki.Application.UseCases.Titles.Commands.CheckLevelTitles
{
    public class CheckLevelTitlesCommand : IRequest<OperationResult<List<Title>>>
    {
        public string UserId { get; set; } = string.Empty;
    }
}
