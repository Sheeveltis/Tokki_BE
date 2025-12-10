using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Titles.Commands.CreateTitle
{
    public class CreateTitleCommand : IRequest<OperationResult<Title>>
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public long RequiredXP { get; set; }
        public string ColorHex { get; set; } = "#000000";
        public string? IconUrl { get; set; }
        public bool IsSystemGiven { get; set; }
    }
}