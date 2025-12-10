using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Titles.Commands.UpdateTitle
{
    public class UpdateTitleCommand : IRequest<OperationResult<Title>>
    {
        public int TitleId { get; set; } 
        public string Name { get; set; }
        public string? Description { get; set; }
        public long RequiredXP { get; set; }
        public string ColorHex { get; set; }
        public string? IconUrl { get; set; }
        public bool IsSystemGiven { get; set; }
    }
}