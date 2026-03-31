using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Titles.Commands.EquipTitle
{
    public class EquipTitleCommand : IRequest<OperationResult<bool>>
    {
        public string TitleId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty; // To be set from controller
    }
}
