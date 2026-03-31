using System.Collections.Generic;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Accounts.Commands.UpdateMyLevel
{
    public class UpdateMyLevelResponse
    {
        public bool Success { get; set; }
        public List<Title> NewlyUnlockedTitles { get; set; } = new List<Title>();
    }
}
